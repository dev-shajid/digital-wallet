using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Auth.Models;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
