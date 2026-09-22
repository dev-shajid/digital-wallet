using Microsoft.EntityFrameworkCore;
using Wallet.Application.Common.Exceptions;
using Wallet.Application.Common.Interfaces;
using Wallet.Application.Common.Models;
using Wallet.Domain.Entities;
using Wallet.Domain.Enums;

namespace Wallet.Application.Features.Auth;

public class AuthService(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAccountNumberGenerator accountNumberGenerator,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ValidateRegisterRequest(request);

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Users.AnyAsync(u => u.Email.ToLower() == emailLower, ct);
        if (emailExists)
        {
            throw new ValidationException(
                "A user with this email address already exists.",
                [new ApiError("Email", "Email is already registered.")]);
        }

        var bdtCurrency = await dbContext.Currencies.FirstOrDefaultAsync(c => c.Code == "BDT", ct)
            ?? throw new InvalidOperationException("Default BDT currency is not configured in the database.");

        string accountNo;
        do
        {
            accountNo = accountNumberGenerator.Generate();
        }
        while (await dbContext.Users.AnyAsync(u => u.AccountNo == accountNo, ct));

        var passwordHash = passwordHasher.HashPassword(request.Password);

        await using var transaction = await dbContext.BeginTransactionAsync(ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = emailLower,
            AccountNo = accountNo,
            PasswordHash = passwordHash,
            Role = Role.USER,
        };

        dbContext.Users.Add(user);

        var defaultWallet = new Domain.Entities.Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CurrencyId = bdtCurrency.Id,
            Balance = 0m,
            Status = WalletStatus.ACTIVE,
        };

        dbContext.Wallets.Add(defaultWallet);

        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var token = tokenService.GenerateToken(user);

        return new AuthResponse(
            token,
            new UserDto(user.Id, user.Name, user.Email, user.AccountNo, user.Role.ToString(), user.CreatedAt));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException(
                "Email and password are required.",
                [new ApiError("Credentials", "Email and password are required.")]);
        }

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower, ct);

        if (user == null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ValidationException(
                "Invalid email or password.",
                [new ApiError("Credentials", "Invalid email or password.")]);
        }

        var token = tokenService.GenerateToken(user);

        return new AuthResponse(
            token,
            new UserDto(user.Id, user.Name, user.Email, user.AccountNo, user.Role.ToString(), user.CreatedAt));
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(CancellationToken ct = default)
    {
        var currentUserId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await dbContext.Users
            .Include(u => u.Wallets)
            .ThenInclude(w => w.Currency)
            .FirstOrDefaultAsync(u => u.Id == currentUserId, ct)
            ?? throw new NotFoundException($"User with ID '{currentUserId}' was not found.");

        var wallets = user.Wallets
            .OrderBy(w => w.CreatedAt)
            .Select(w => new UserWalletDto(
                w.Id,
                w.CurrencyId,
                w.Currency.Code,
                w.Currency.Symbol,
                w.Balance,
                w.Status.ToString()))
            .ToList();

        return new UserProfileDto(
            user.Id,
            user.Name,
            user.Email,
            user.AccountNo,
            user.Role.ToString(),
            user.CreatedAt,
            wallets);
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<ApiError>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ApiError("Name", "Name is required."));
        }
        else if (request.Name.Trim().Length > 200)
        {
            errors.Add(new ApiError("Name", "Name cannot exceed 200 characters."));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ApiError("Email", "Email is required."));
        }
        else if (!request.Email.Contains('@') || !request.Email.Contains('.'))
        {
            errors.Add(new ApiError("Email", "Email is in an invalid format."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(new ApiError("Password", "Password is required."));
        }
        else if (request.Password.Length < 6)
        {
            errors.Add(new ApiError("Password", "Password must be at least 6 characters long."));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Registration request is invalid.", errors);
        }
    }
}
