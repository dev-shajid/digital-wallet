using WalletSystem.Domain.Entities;

namespace WalletSystem.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    int ExpirationMinutes { get; }
}
