namespace WalletSystem.Domain.Enums;

/// <summary>A currency that is in use is never deleted - it's just marked INACTIVE.</summary>
public enum CurrencyStatus
{
    ACTIVE,
    INACTIVE
}
