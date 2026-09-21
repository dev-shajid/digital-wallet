using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class Wallet : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string CurrencyCode { get; set; }

    public Currency Currency { get; set; } = null!;

    public decimal Balance { get; set; }

    public WalletStatus Status { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];

    public ICollection<P2PTransfer> ReceivedP2PTransfers { get; set; } = [];
}
