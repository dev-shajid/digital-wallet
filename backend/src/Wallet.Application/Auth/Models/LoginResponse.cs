namespace WalletSystem.Application.Auth.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public int RefreshTokenExpiresIn { get; set; }
    public RegisterResponse User { get; set; } = null!;
}
