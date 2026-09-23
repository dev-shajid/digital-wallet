using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Application.Abstractions;

public interface IAuthService
{
    /// <summary>Returns true if a user with this email already exists in the database.</summary>
    Task<bool> IsEmailTakenAsync(string normalizedEmail, CancellationToken ct = default);

    /// <summary>
    /// Creates the user + default BDT wallet + refresh token in one DB transaction
    /// and returns a JWT. Called after OTP has been verified.
    /// Accepts pre-validated, pre-hashed data — no re-hashing happens here.
    /// </summary>
    Task<ApiResponse<LoginResponse>> CompleteRegistrationAsync(PendingRegistrationData data, CancellationToken ct = default);

    // ── Kept for backward compatibility while teammates migrate ──
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<object?>> LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<ApiResponse<RegisterResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
