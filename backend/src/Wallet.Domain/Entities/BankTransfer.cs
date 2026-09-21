namespace Wallet.Domain.Entities;

public class BankTransfer
{
    public Guid TransactionId { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public required string BankCode { get; set; }

    public required string BankReference { get; set; }
}
