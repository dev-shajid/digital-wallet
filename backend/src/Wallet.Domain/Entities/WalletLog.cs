using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>
/// An append-only ledger row: one row per wallet involved in a transaction (a P2P transfer
/// creates two rows - a DEBIT on the sender's wallet and a CREDIT on the receiver's wallet).
/// Rows are created PENDING (BalanceBefore/After left null) and are only ever updated once,
/// when the leg is actually applied - never deleted, never changed after that.
/// </summary>
public class WalletLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public WalletLogDirection Direction { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Wallet balance right before this leg was applied. Null until applied.</summary>
    public decimal? BalanceBefore { get; set; }

    /// <summary>Wallet balance right after this leg was applied. Null until applied.</summary>
    public decimal? BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
