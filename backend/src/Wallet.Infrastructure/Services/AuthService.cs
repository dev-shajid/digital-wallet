using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Persistence.Seed;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Handles authentication operations including atomic user registration and login.
/// </summary>
public class AuthService : IAuthService
{
    private readonly WalletDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountNumberGenerator _accountNumberGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        WalletDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAccountNumberGenerator accountNumberGenerator,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _accountNumberGenerator = accountNumberGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    /// <summary>
    /// Atomically registers a new user and creates their default BDT wallet inside a single database transaction,
    /// then logs them in immediately by returning a JWT alongside the created profile.
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

        // 6. ATOMIC TRANSACTION: Save User + Wallet in one atomic database transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            await _dbContext.Users.AddAsync(user, ct);
            await _dbContext.Wallets.AddAsync(wallet, ct);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        // 7. Issue a token immediately so the caller is logged in on registration
        string token = _jwtTokenGenerator.GenerateToken(user);

        var responseData = new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenGenerator.ExpirationMinutes * 60,
            User = new RegisterResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                AccountNo = user.AccountNo,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        };

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = "User registered successfully.",
            Data = responseData
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

        // 3. Generate JWT access token
        string token = _jwtTokenGenerator.GenerateToken(user);

        // 4. Prepare response
        var responseData = new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtTokenGenerator.ExpirationMinutes * 60,
            User = new RegisterResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                AccountNo = user.AccountNo,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        };

        return new ApiResponse<LoginResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Login successful.",
            Data = responseData
        };
    }
}
