namespace Wallet.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<UserProfileDto> GetCurrentUserProfileAsync(CancellationToken ct = default);
}
