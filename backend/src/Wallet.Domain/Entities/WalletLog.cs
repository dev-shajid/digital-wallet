using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class WalletLog
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Wallet Wallet { get; set; } = null!;

    public Guid TransactionId { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public WalletLogDirection Direction { get; set; }

    public decimal Amount { get; set; }

    public decimal? BalanceBefore { get; set; }

    public decimal? BalanceAfter { get; set; }

    public DateTime CreatedAt { get; set; }
}
