namespace WalletSystem.Domain.Enums;

/// <summary>
/// FROZEN blocks every transaction on the wallet. CLOSED requires the balance to be 0
/// first (enforced by application logic, not the database).
/// </summary>
public enum WalletStatus
{
    ACTIVE,
    FROZEN,
    CLOSED
}
