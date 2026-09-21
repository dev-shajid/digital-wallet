using WalletApp.Models.Enums;

namespace WalletApp.Models.Entities;

public class Transaction : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public TransactionStatus Status { get; set; }

    public required string Reference { get; set; }

    public string? Note { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureReason { get; set; }

    public BankTransfer? BankTransfer { get; set; }

    public P2PTransfer? P2PTransfer { get; set; }

    public Expense? Expense { get; set; }

    public ICollection<WalletLog> WalletLogs { get; set; } = [];
}
