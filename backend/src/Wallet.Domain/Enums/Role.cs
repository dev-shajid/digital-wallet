namespace WalletSystem.Domain.Enums;

/// <summary>What a user is allowed to do. Stored in the database as text ("USER", "ADMIN"), not a number.</summary>
public enum Role
{
    USER,
    ADMIN
}
