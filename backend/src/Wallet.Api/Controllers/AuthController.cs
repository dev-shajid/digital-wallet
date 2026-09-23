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
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(
        IAuthService authService,
        IOtpService otpService,
        IPasswordHasher passwordHasher)
    {
        _authService = authService;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
    }

    // ─────────────────────────────────────────────────────────────
    // STEP 1 OF REGISTRATION: validate form → send OTP
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// First step of registration. Validates the form fields, stores the data
    /// temporarily in cache, and sends a 6-digit OTP to the provided email.
    /// No user is created in the database at this point.
    /// </summary>
    [HttpPost("registration")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> InitiateRegistration([FromBody] RegisterRequest request)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Reject if a user with this email already exists in the database.
        var emailTaken = await _authService.IsEmailTakenAsync(normalizedEmail);
        if (emailTaken)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Status = StatusCodes.Status409Conflict,
                Message = "A user with this email address already exists."
            });
        }

        // Hash the password NOW before storing in cache — plain-text never sits in memory.
        var pendingData = new PendingRegistrationData(
            Name: request.Name.Trim(),
            Email: normalizedEmail,
            PasswordHash: _passwordHasher.HashPassword(request.Password)
        );

        await _otpService.InitiateRegistrationAsync(pendingData);

        return Accepted(new ApiResponse<object>
        {
            Success = true,
            Status = StatusCodes.Status202Accepted,
            Message = "A 6-digit verification code has been sent to your email. It expires in 10 minutes."
        });
    }

    // ─────────────────────────────────────────────────────────────
    // STEP 2 OF REGISTRATION: verify OTP → create user → issue JWT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Second step of registration. The user submits the OTP they received by email.
    /// On success the user + wallet are created and a JWT is returned immediately.
    /// </summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status410Gone)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var validationResult = _otpService.ValidateOtp(normalizedEmail, request.Otp);

        if (validationResult == OtpValidationResult.Expired)
        {
            // 410 Gone — the OTP window has closed, user must request a new one.
            return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>
            {
                Success = false,
                Status = StatusCodes.Status410Gone,
                Message = "Your verification code has expired. Please request a new one."
            });
        }

        if (validationResult == OtpValidationResult.InvalidCode)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Status = StatusCodes.Status400BadRequest,
                Message = "Incorrect verification code. Please try again."
            });
        }

        // OTP is valid — retrieve the registration data that was stored in cache.
        var pendingData = _otpService.GetPendingRegistration(normalizedEmail);
        if (pendingData is null)
        {
            // Extremely rare race condition: OTP marked valid but cache entry disappeared.
            return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>
            {
                Success = false,
                Status = StatusCodes.Status410Gone,
                Message = "Session data has expired. Please restart the registration process."
            });
        }

        // Delegate user + wallet creation and JWT issuance to AuthService.
        var result = await _authService.CompleteRegistrationAsync(pendingData);

        // Clean up the cache entry regardless of result — no dangling data.
        _otpService.ClearPendingRegistration(normalizedEmail);

        return StatusCode(result.Status, result);
    }

    // ─────────────────────────────────────────────────────────────
    // RESEND OTP (user didn't get it or it expired)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a fresh OTP and resends it to the email.
    /// Only works while a pending registration still exists in cache.
    /// If the session expired the user must call initiate-registration again.
    /// </summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> ResendOtp([FromBody] ResendOtpRequest request)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var sent = await _otpService.ResendOtpAsync(normalizedEmail);

        if (!sent)
        {
            // Cache entry is gone — the original initiate-registration session expired.
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Status = StatusCodes.Status404NotFound,
                Message = "No pending registration found for this email. Please fill in the registration form again."
            });
        }

        return Accepted(new ApiResponse<object>
        {
            Success = true,
            Status = StatusCodes.Status202Accepted,
            Message = "A new verification code has been sent to your email."
        });
    }

    // ─────────────────────────────────────────────────────────────
    // EXISTING AUTH ENDPOINTS (unchanged)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Authenticates a user with email and password and returns a JWT access token plus a refresh token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Exchanges a still-valid refresh token for a new access + refresh token pair. The refresh
    /// token used is rotated (revoked) as part of this call.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Revokes a refresh token so it can no longer be used to obtain new access tokens.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request);
        return StatusCode(result.Status, result);
    }
}