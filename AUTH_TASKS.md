# Auth & User Management: 3-Member Task Breakdown

## Execution Sequence
```
Member 1 (Foundation & Contracts)  ──►  Member 2 (JWT & Login) + Member 3 (Register & Controller) in Parallel
```

---

## 👤 Member 1: Contracts, DTOs & Security Utilities (MUST DO FIRST)
- [x] Create `RegisterRequest.cs` in `backend/src/Wallet.Application/Auth/Models/` with validation rules for name, email, and password.
- [x] Create `RegisterResponse.cs` in `backend/src/Wallet.Application/Auth/Models/` with user summary fields (`UserId`, `Name`, `Email`, `AccountNo`, `Role`, `CreatedAt`).
- [x] Create `LoginRequest.cs` in `backend/src/Wallet.Application/Auth/Models/` with email and password fields.
- [x] Create `LoginResponse.cs` in `backend/src/Wallet.Application/Auth/Models/` with token, token type, expiry, and user summary.
- [x] Create `IAuthService.cs` in `backend/src/Wallet.Application/Abstractions/` declaring `RegisterAsync` and `LoginAsync` returning `ApiResponse<T>`.
- [x] Create `IPasswordHasher.cs` in `backend/src/Wallet.Application/Abstractions/` declaring `HashPassword` and `VerifyPassword`.
- [x] Create `IAccountNumberGenerator.cs` in `backend/src/Wallet.Application/Abstractions/` declaring `GenerateAccountNumber` and `ValidateAccountNumber`.
- [x] Create `IJwtTokenGenerator.cs` in `backend/src/Wallet.Application/Abstractions/` declaring `GenerateToken` and `ExpirationMinutes`.
- [x] Create `PasswordHasher.cs` in `backend/src/Wallet.Infrastructure/Services/` implementing PBKDF2/SHA-256 with 100k iterations and salt.
- [x] Create `AccountNumberGenerator.cs` in `backend/src/Wallet.Infrastructure/Services/` implementing 10-digit random number generation with Luhn check digit algorithm.

---

## 👤 Member 2: JWT & Sign-In Flow (Starts after Member 1)
- [ ] Create `JwtSettings.cs` in `backend/src/Wallet.Infrastructure/Authentication/` with `Secret`, `Issuer`, `Audience`, and `ExpirationMinutes`.
- [ ] Add JWT configuration section to `backend/src/Wallet.Api/appsettings.json` and `appsettings.Development.json`.
- [ ] Create `JwtTokenGenerator.cs` in `backend/src/Wallet.Infrastructure/Services/` issuing signed tokens with `sub`, `email`, `role`, and `accountNo` claims.
- [ ] Implement `LoginAsync` method in `backend/src/Wallet.Infrastructure/Services/AuthService.cs` to query user, verify password hash, and return JWT `LoginResponse`.
- [ ] Configure `JwtBearer` authentication middleware and Swagger Bearer security definitions in `backend/src/Wallet.Api/Program.cs`.

---

## 👤 Member 3: Atomic Sign-Up Flow, Controller & DI (Starts after Member 1)
- [ ] Implement `RegisterAsync` method in `backend/src/Wallet.Infrastructure/Services/AuthService.cs` to check email uniqueness and hash password.
- [ ] Wrap User creation and default BDT Wallet creation inside a single `IDbContextTransaction` in `AuthService.RegisterAsync` to ensure atomic sign-up.
- [ ] Update `backend/src/Wallet.Api/Controllers/AuthController.cs` to fix namespaces and wire `Register` and `Login` HTTP endpoints.
- [ ] Register `IAuthService`, `IPasswordHasher`, `IAccountNumberGenerator`, and `IJwtTokenGenerator` in `backend/src/Wallet.Infrastructure/DependencyInjection.cs`.
