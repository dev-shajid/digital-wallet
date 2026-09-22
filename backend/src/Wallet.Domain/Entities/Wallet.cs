using WalletSystem.Domain.Common;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>
/// One user's balance in one currency. A user can have several wallets, but only one per
/// currency (enforced by a UNIQUE(userId, currencyId) constraint in the database).
///
/// IMPORTANT: nothing should ever set Balance directly outside of the ledger service that
/// is built in a later phase. It is a cached/denormalized number that must always match the
/// sum of applied WalletLog entries for this wallet - that's what keeps balances correct
/// under concurrent requests.
/// </summary>
public class Wallet : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public decimal Balance { get; set; }

    public WalletStatus Status { get; set; } = WalletStatus.ACTIVE;

    public ICollection<WalletLog> WalletLogs { get; set; } = new List<WalletLog>();

    /// <summary>P2P transfers received INTO this wallet (this wallet is the receiver).</summary>
    public ICollection<P2PTransfer> IncomingP2PTransfers { get; set; } = new List<P2PTransfer>();
}
