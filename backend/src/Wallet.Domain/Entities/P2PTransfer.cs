namespace WalletSystem.Domain.Entities;

/// <summary>
/// Extra detail for a P2P_TRANSFER Transaction (1:1, keyed by TransactionId like BankTransfer).
/// The sender is Transaction.UserId and the sender's wallet is the DEBIT row in WalletLog;
/// this table stores the RECEIVER's wallet separately so "money I received" history can be
/// queried directly, without having to search every user's WalletLog for CREDIT rows.
/// </summary>
public class P2PTransfer
{
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public Guid ReceiverWalletId { get; set; }
    public Wallet ReceiverWallet { get; set; } = null!;
}
