namespace WalletSystem.Application.Auth.Models;

/// <summary>
/// The three possible outcomes when a user submits an OTP.
/// Using an enum instead of bool lets the controller return
/// a precise error message for each case.
/// </summary>
public enum OtpValidationResult
{
    /// <summary>OTP matched and has not expired. User can proceed.</summary>
    Success,

    /// <summary>
    /// OTP is still alive in the cache but the submitted code does not match.
    /// User should try entering the code again.
    /// </summary>
    InvalidCode,

    /// <summary>
    /// No entry found in the cache for this email — the OTP has expired (or was
    /// never sent). User must request a new one via resend-otp.
    /// </summary>
    Expired
}
