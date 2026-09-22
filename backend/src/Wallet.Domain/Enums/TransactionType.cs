namespace WalletSystem.Domain.Enums;

/// <summary>
/// What kind of money movement a Transaction represents.
/// CASH_OUT is reserved for a future phase and is not implemented yet.
/// </summary>
public enum TransactionType
{
    CASH_IN,
    CASH_OUT,
    P2P_TRANSFER,
    EXPENSE
}
