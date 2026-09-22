using System.Security.Cryptography;
using System.Text;
using WalletSystem.Application.Abstractions;

namespace WalletSystem.Infrastructure.Services;

public class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenBytes = 32; // 256 bits of entropy

    public string GenerateToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));

    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
