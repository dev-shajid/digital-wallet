using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transfers.Models;

namespace WalletSystem.Application.Abstractions;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<object?>> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<RegisterResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<AccountLookupResponse>> LookupAccountAsync(string accountNo, CancellationToken ct = default);
}
