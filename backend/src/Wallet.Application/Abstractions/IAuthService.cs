using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Application.Abstractions;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
