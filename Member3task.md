📋 Member 3-এর সিকুয়েন্স টাস্ক লিস্ট (One-line per task):
- [x] Create AuthService.cs in backend/src/Wallet.Infrastructure/Services/ implementing IAuthService with atomic RegisterAsync (User + BDT Wallet in single DB transaction).
- [x] Implement LoginAsync in AuthService.cs using IPasswordHasher to verify password and IJwtTokenGenerator to issue JWT token.
- [x] Update backend/src/Wallet.Api/Controllers/AuthController.cs to use correct namespaces (WalletSystem.*) and route requests to IAuthService.
- [x] Register IAuthService, IPasswordHasher, IAccountNumberGenerator, and IJwtTokenGenerator in backend/src/Wallet.Infrastructure/DependencyInjection.cs.