using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Auth.Models;

/// <summary>
/// Body for POST /auth/verify-email.
/// The frontend sends the email (to look up the cached OTP) and the OTP the user typed.
/// </summary>
public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;
}
