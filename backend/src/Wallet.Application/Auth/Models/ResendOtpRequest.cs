using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Auth.Models;

/// <summary>
/// Body for POST /auth/resend-otp.
/// The frontend only needs to send the email — the backend looks up the existing
/// pending registration data in cache and sends a fresh OTP to that address.
/// </summary>
public class ResendOtpRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;
}
