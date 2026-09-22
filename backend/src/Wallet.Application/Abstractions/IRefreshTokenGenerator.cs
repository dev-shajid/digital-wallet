namespace WalletSystem.Application.Abstractions;

public interface IRefreshTokenGenerator
{
    /// <summary>A new cryptographically random opaque token, given to the client raw.</summary>
    string GenerateToken();

    /// <summary>The SHA-256 hex digest of a token, the only form ever stored in the database.</summary>
    string Hash(string token);
}
