using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Authentication;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Seed;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Handles authentication operations including atomic user registration, login, refresh-token
/// rotation, logout, and reading the current user's own profile.
/// </summary>
public class AuthService : IAuthService
{
    private readonly WalletDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        WalletDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAccountNumberGenerator accountNumberGenerator,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IOptions<JwtSettings> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _accountNumberGenerator = accountNumberGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>Returns true if a user with this normalised email already exists.</summary>
    public async Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken ct = default)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == normalizedEmail, ct);
    }

    /// <summary>
    /// Creates the user + default BDT wallet + refresh token in one DB transaction using
    /// pre-validated, pre-hashed data supplied by the OTP verification flow.
    /// </summary>
    public async Task<ApiResponse<LoginResponse>> CompleteRegistrationAsync(
        PendingRegistrationData data, CancellationToken ct = default)
    {
        string accountNo = await _accountNumberGenerator.GenerateAccountNumberAsync(ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = data.Name,
            Email = data.Email,
            AccountNo = accountNo,
            PasswordHash = data.PasswordHash,   // already hashed by the controller
            Role = Role.USER
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 0m,
            Status = WalletStatus.ACTIVE
        };

        var (session, refreshTokenEntity) = BuildSession(user);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.Wallets.AddAsync(wallet, ct);
            await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity, ct);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = "Account created successfully.",
            Data = session
        };
    }

    /// <summary>
    /// Atomically registers a new user and creates their default BDT wallet inside a single database transaction,
    /// then logs them in immediately by returning a JWT (plus refresh token) alongside the created profile.
    /// </summary>
    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Check if email already exists
        bool emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (emailExists)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Status = StatusCodes.Status409Conflict,
                Message = "A user with this email address already exists.",
                Data = null
            };
        }

        // 2. Generate the next "AC"-prefixed account number from the DB sequence
        string accountNo = await _accountNumberGenerator.GenerateAccountNumberAsync(ct);

        // 3. Hash the password
        string passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Create User entity
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            AccountNo = accountNo,
            PasswordHash = passwordHash,
            Role = Role.USER
        };

        // 5. Create default BDT Wallet entity
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 0m,
            Status = WalletStatus.ACTIVE
        };

        // 6. Issue the session's tokens up front so the refresh token can be inserted in the
        // same atomic transaction as the user and wallet.
        var (session, refreshTokenEntity) = BuildSession(user);

        // 7. ATOMIC TRANSACTION: Save User + Wallet + RefreshToken in one atomic database transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.Wallets.AddAsync(wallet, ct);
            await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity, ct);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = "User registered successfully.",
            Data = session
        };
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Find user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        // 2. Validate user existence and password hash
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Status = StatusCodes.Status401Unauthorized,
                Message = "Invalid email or password.",
                Data = null
            };
        }

        // 3. Issue a fresh access + refresh token pair
        var (session, refreshTokenEntity) = BuildSession(user);
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity, ct);
        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Login successful.",
            Data = session
        };
    }

    /// <summary>
    /// Redeems a refresh token for a brand new access + refresh token pair, rotating (revoking)
    /// the one just used. If a token that was already rotated is presented again, that's a sign
    /// it was stolen and replayed, so every outstanding refresh token for that user is revoked,
    /// forcing a fresh login everywhere.
    /// </summary>
    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        string incomingHash = _refreshTokenGenerator.Hash(request.RefreshToken);

        var existing = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == incomingHash, ct);

        if (existing is null)
        {
            return Unauthorized("Invalid refresh token.");
        }

        if (existing.RevokedAt is not null)
        {
            // A rotated token being presented again is a strong signal it was stolen and
            // replayed - a legitimate client only ever uses the newest token it was given.
            // A token revoked by an explicit logout reused later is just stale, not theft,
            // so it doesn't get the same treatment.
            if (existing.ReplacedByTokenHash is not null)
            {
                var activeFamilyTokens = await _dbContext.RefreshTokens
                    .Where(rt => rt.UserId == existing.UserId && rt.RevokedAt == null)
                    .ToListAsync(ct);

                var now = DateTime.UtcNow;
                foreach (var token in activeFamilyTokens)
                {
                    token.RevokedAt = now;
                    token.UpdatedAt = now;
                }
                await _dbContext.SaveChangesAsync(ct);

                return Unauthorized("This refresh token has already been used. All sessions for this account have been signed out for safety.");
            }

            return Unauthorized("This refresh token has been revoked. Please log in again.");
        }

        if (existing.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized("Refresh token has expired. Please log in again.");
        }

        // Rotate: issue a fresh pair and revoke the one just redeemed, in a single SaveChanges
        // call so both changes commit together.
        var (session, refreshTokenEntity) = BuildSession(existing.User);
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity, ct);

        existing.RevokedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.ReplacedByTokenHash = refreshTokenEntity.TokenHash;

        await _dbContext.SaveChangesAsync(ct);

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Token refreshed successfully.",
            Data = session
        };
    }

    /// <summary>Revokes a refresh token so it can no longer be redeemed. Idempotent and never reveals whether the token existed.</summary>
    public async Task<ApiResponse<object?>> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        string hash = _refreshTokenGenerator.Hash(request.RefreshToken);

        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }

        return new ApiResponse<object?>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Logged out successfully.",
            Data = null
        };
    }

    public async Task<ApiResponse<RegisterResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FindAsync([userId], ct);

        if (user is null)
        {
            return new ApiResponse<RegisterResponse>
            {
                Success = false,
                Status = StatusCodes.Status404NotFound,
                Message = "User not found.",
                Data = null
            };
        }

        return new ApiResponse<RegisterResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Profile retrieved successfully.",
            Data = ToProfile(user)
        };
    }

    /// <summary>Builds the access + refresh token pair for a session. The refresh token entity is returned un-saved - the caller controls the transaction.</summary>
    private (LoginResponse Session, RefreshToken RefreshTokenEntity) BuildSession(User user)
    {
        string accessToken = _jwtTokenGenerator.GenerateToken(user);
        string refreshTokenRaw = _refreshTokenGenerator.GenerateToken();
        string refreshTokenHash = _refreshTokenGenerator.Hash(refreshTokenRaw);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshTokenExpiresAt
        };

        var session = new LoginResponse
        {
            Token = accessToken,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenGenerator.ExpirationMinutes * 60,
            RefreshToken = refreshTokenRaw,
            RefreshTokenExpiresIn = _jwtSettings.RefreshTokenExpirationDays * 24 * 60 * 60,
            User = ToProfile(user)
        };

        return (session, refreshTokenEntity);
    }

    private static RegisterResponse ToProfile(User user) => new()
    {
        UserId = user.Id,
        Name = user.Name,
        Email = user.Email,
        AccountNo = user.AccountNo,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };

    private static ApiResponse<LoginResponse> Unauthorized(string message) => new()
    {
        Success = false,
        Status = StatusCodes.Status401Unauthorized,
        Message = message,
        Data = null
    };
}
