using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class Transaction : AuditableEntity
{
    public Guid WalletId { get; set; }

    public Wallet Wallet { get; set; } = null!;

    public required string CurrencyCode { get; set; }

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
}
