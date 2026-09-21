namespace Wallet.Domain.Entities;

public class P2PTransfer
{
    public Guid TransactionId { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public Guid ReceiverWalletId { get; set; }

    public Wallet ReceiverWallet { get; set; } = null!;
}
