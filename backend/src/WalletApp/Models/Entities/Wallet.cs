using WalletApp.Models.Enums;

namespace WalletApp.Models.Entities;

public class Wallet : AuditableEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public decimal Balance { get; set; }

    public WalletStatus Status { get; set; }

    public ICollection<WalletLog> WalletLogs { get; set; } = [];

    public ICollection<P2PTransfer> ReceivedP2PTransfers { get; set; } = [];
}
