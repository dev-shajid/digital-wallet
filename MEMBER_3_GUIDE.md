# Member 3: Atomic Sign-Up Flow, Controller & Dependency Injection

This guide explains **Member 3's** exact responsibilities, sequential task list, and complete ready-to-use C# code.

---

## 🎯 Member 3-এর কাজের সহজ ব্যাখ্যা (Overview)

মেম্বার ৩-এর মূল কাজ হলো **User Registration (Sign-up)** সম্পন্ন করা, **User + BDT Wallet একসাথে ডেটাবেসে নিশ্চিত করা**, **API Controller ঠিক করা**, এবং সব সার্ভিসকে **Dependency Injection**-এ যুক্ত করা।

### ১. Atomic Sign-Up (User + Wallet এক সাথে সেভ হওয়া) কেন এত গুরুত্বপূর্ণ?
আমাদের সিস্টেমের নিয়ম হলো: **"প্রত্যেক রেজিস্ট্রেশন করা ইউজারের অবশ্যই ১টি BDT ওয়ালেট থাকতে হবে।"**
* যদি কোনো কারণে ইউজার তৈরি হলো কিন্তু ওয়ালেট তৈরির সময় সার্ভার ক্র্যাশ করল, তাহলে সেই ইউজার আর কখনো ওয়ালেট পাবে না এবং পুরো সিস্টেম ভেঙে যাবে।
* তাই আমরা ডেটাবেস ট্রানজেকশন (`BeginTransactionAsync`) ব্যবহার করি। ইউজার এবং BDT ওয়ালেট—**দুটোই সফল হলে ডেটাবেসে সেভ হবে, আর একটিও ফেইল করলে পুরোটা বাতিল (Rollback) হয়ে যাবে।**

### ২. `AuthService.RegisterAsync`-এর ধাপসমূহ:
1. চেক করবে এই ইমেইল দিয়ে আগে কেউ অ্যাকাউন্ট খুলেছে কিনা (থাকলে এরর দিবে)।
2. মেম্বার ১-এর `IAccountNumberGenerator` দিয়ে ১০ ডিজিটের Luhn অ্যাকাউন্ট নম্বর তৈরি করবে।
3. মেম্বার ১-এর `IPasswordHasher` দিয়ে পাসওয়ার্ড হ্যাশ করবে।
4. একটি ডেটাবেস ট্রানজেকশনের ভেতরে `User` এবং তার জন্য `0` ব্যালেন্সের `BDT Wallet` একসাথে সেভ করবে।
5. ক্লায়েন্টকে `201 Created` স্ট্যাটাস সহ `RegisterResponse` পাঠাবে।

---

## 📋 Sequential Task List (1 Line Per Task)

- [ ] Create `AuthService.cs` in `backend/src/Wallet.Infrastructure/Services/` implementing `IAuthService` with atomic `RegisterAsync` (User + BDT Wallet in single DB transaction).
- [ ] Implement `LoginAsync` in `AuthService.cs` using `IPasswordHasher` to verify password and `IJwtTokenGenerator` to issue JWT token.
- [ ] Update `backend/src/Wallet.Api/Controllers/AuthController.cs` to use correct namespaces (`WalletSystem.*`) and route requests to `IAuthService`.
- [ ] Register `IAuthService`, `IPasswordHasher`, `IAccountNumberGenerator`, and `IJwtTokenGenerator` in `backend/src/Wallet.Infrastructure/DependencyInjection.cs`.

---

## 💻 Member 3 Complete Code Files

### File 1: `src/Wallet.Infrastructure/Services/AuthService.cs`
```csharp
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

    public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Check if email is already registered
        bool emailExists = await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (emailExists)
        {
            return new ApiResponse<RegisterResponse>
            {
                Success = false,
                Status = StatusCodes.Status409Conflict,
                Message = "A user with this email address already exists.",
                Data = null
            };
        }

        // 2. Generate unique 10-digit account number with Luhn check digit
        string accountNo;
        int maxRetries = 5;
        int attempt = 0;

        do
        {
            accountNo = _accountNumberGenerator.GenerateAccountNumber();
            bool accountExists = await _dbContext.Users.AnyAsync(u => u.AccountNo == accountNo, ct);
            if (!accountExists) break;

            attempt++;
            if (attempt >= maxRetries)
            {
                return new ApiResponse<RegisterResponse>
                {
                    Success = false,
                    Status = StatusCodes.Status500InternalServerError,
                    Message = "Failed to generate a unique account number. Please try again.",
                    Data = null
                };
            }
        } while (true);

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

        // 6. ATOMIC COMMIT: Save User + Wallet in one database transaction
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

        // 7. Prepare response
        var responseData = new RegisterResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            AccountNo = user.AccountNo,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };

        return new ApiResponse<RegisterResponse>
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
```

---

### File 2: `src/Wallet.Api/Controllers/AuthController.cs`
```csharp
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/v1/auth/register
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode(result.Status, result);
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode(result.Status, result);
    }
}
```

---

### File 3: Update `src/Wallet.Infrastructure/DependencyInjection.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Abstractions;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Infrastructure.Services;

namespace WalletSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:Default' configuration value.");

        services.AddDbContext<WalletDbContext>(options =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());

        // Register Member 1, 2, 3 services into DI
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccountNumberGenerator, AccountNumberGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
```
