namespace WalletSystem.Application.Auth.Models;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
