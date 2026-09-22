using WalletSystem.Domain.Common;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>
/// One row per money movement, owned by the user who started it (Transaction.UserId).
/// This is the "parent" record - which wallet(s) were actually debited/credited lives in
/// WalletLog, and type-specific details live in one of BankTransfer / P2PTransfer / Expense.
/// </summary>
public class Transaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.PENDING;

    /// <summary>Unique human-readable reference, e.g. shown to the user as a receipt id.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Optional note the user typed in (e.g. an expense description).</summary>
    public string? Note { get; set; }

    /// <summary>Short machine-readable code (e.g. "BANK_DECLINED"). Visible to normal users.</summary>
    public string? FailureCode { get; set; }

    /// <summary>Full failure detail (e.g. raw bank error). Admin-only - never returned to normal users.</summary>
    public string? FailureReason { get; set; }

    public ICollection<WalletLog> WalletLogs { get; set; } = new List<WalletLog>();

    // 1:1 side tables - at most one of these is set, depending on Type.
    public BankTransfer? BankTransfer { get; set; }
    public P2PTransfer? P2PTransfer { get; set; }
    public Expense? Expense { get; set; }
}
