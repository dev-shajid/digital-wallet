# Member 1 Implementation Guide: Contracts, DTOs & Security Utilities

This guide contains everything **Member 1** needs to create. All code here is pure C#, requires zero database access, and can be created directly and pushed.

---

## 📁 File Structure Overview

You will create **8 files** across two projects:

```
backend/src/
├── Wallet.Application/
│   ├── Abstractions/
│   │   ├── IAuthService.cs
│   │   ├── IPasswordHasher.cs
│   │   ├── IAccountNumberGenerator.cs
│   │   └── IJwtTokenGenerator.cs
│   └── Auth/
│       └── Models/
│           ├── RegisterRequest.cs
│           ├── RegisterResponse.cs
│           ├── LoginRequest.cs
│           └── LoginResponse.cs
└── Wallet.Infrastructure/
    └── Services/
        ├── PasswordHasher.cs
        └── AccountNumberGenerator.cs
```

---

## 1. DTOs (`Wallet.Application/Auth/Models/`)

### File 1: `src/Wallet.Application/Auth/Models/RegisterRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Auth.Models;

public class RegisterRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [StringLength(320, ErrorMessage = "Email cannot exceed 320 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;
}
```

---

### File 2: `src/Wallet.Application/Auth/Models/RegisterResponse.cs`
```csharp
namespace WalletSystem.Application.Auth.Models;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

---

### File 3: `src/Wallet.Application/Auth/Models/LoginRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Auth.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}
```

---

### File 4: `src/Wallet.Application/Auth/Models/LoginResponse.cs`
```csharp
namespace WalletSystem.Application.Auth.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public RegisterResponse User { get; set; } = null!;
}
```

---

## 2. Abstractions / Interfaces (`Wallet.Application/Abstractions/`)

### File 5: `src/Wallet.Application/Abstractions/IAuthService.cs`
```csharp
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Application.Abstractions;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
```

---

### File 6: `src/Wallet.Application/Abstractions/IPasswordHasher.cs`
```csharp
namespace WalletSystem.Application.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
```

---

### File 7: `src/Wallet.Application/Abstractions/IAccountNumberGenerator.cs`
```csharp
namespace WalletSystem.Application.Abstractions;

public interface IAccountNumberGenerator
{
    string GenerateAccountNumber();
    bool ValidateAccountNumber(string accountNumber);
}
```

---

### File 8: `src/Wallet.Application/Abstractions/IJwtTokenGenerator.cs`
```csharp
using WalletSystem.Domain.Entities;

namespace WalletSystem.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    int ExpirationMinutes { get; }
}
```

---

## 3. Implementations (`Wallet.Infrastructure/Services/`)

### File 9: `src/Wallet.Infrastructure/Services/PasswordHasher.cs`
```csharp
using System.Security.Cryptography;
using WalletSystem.Application.Abstractions;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Cryptographically secure password hasher using PBKDF2 with HMAC-SHA256 and unique salts.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128-bit salt
    private const int KeySize = 32;        // 256-bit subkey
    private const int Iterations = 100000; // 100,000 PBKDF2 iterations
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            return false;

        string[] parts = passwordHash.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
```

---

### File 10: `src/Wallet.Infrastructure/Services/AccountNumberGenerator.cs`
```csharp
using System.Security.Cryptography;
using System.Text;
using WalletSystem.Application.Abstractions;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Generates 10-digit account numbers where the 10th digit is a Luhn algorithm check digit.
/// </summary>
public class AccountNumberGenerator : IAccountNumberGenerator
{
    public string GenerateAccountNumber()
    {
        // Step 1: Generate 9 random digits (first digit 1-9 to avoid leading zero)
        var sb = new StringBuilder(10);
        sb.Append(RandomNumberGenerator.GetInt32(1, 10)); // First digit: 1-9

        for (int i = 0; i < 8; i++)
        {
            sb.Append(RandomNumberGenerator.GetInt32(0, 10)); // Next 8 digits: 0-9
        }

        string base9Digits = sb.ToString();

        // Step 2: Compute Luhn check digit
        int checkDigit = CalculateLuhnCheckDigit(base9Digits);
        sb.Append(checkDigit);

        return sb.ToString();
    }

    public bool ValidateAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
            return false;

        string base9Digits = accountNumber[..9];
        int expectedCheckDigit = CalculateLuhnCheckDigit(base9Digits);
        int actualCheckDigit = accountNumber[9] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    private static int CalculateLuhnCheckDigit(string digits)
    {
        int sum = 0;

        // Double every second digit from right to left
        for (int i = 0; i < digits.Length; i++)
        {
            int digit = digits[digits.Length - 1 - i] - '0';

            if (i % 2 == 0)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        return (10 - (sum % 10)) % 10;
    }
}
```

---

## 4. Checklist for Member 1

- [ ] Create `RegisterRequest.cs`, `RegisterResponse.cs`, `LoginRequest.cs`, and `LoginResponse.cs` in `src/Wallet.Application/Auth/Models/`
- [ ] Create `IAuthService.cs`, `IPasswordHasher.cs`, `IAccountNumberGenerator.cs`, and `IJwtTokenGenerator.cs` in `src/Wallet.Application/Abstractions/`
- [ ] Create `PasswordHasher.cs` and `AccountNumberGenerator.cs` in `src/Wallet.Infrastructure/Services/`
- [ ] Commit and push feature branch!
