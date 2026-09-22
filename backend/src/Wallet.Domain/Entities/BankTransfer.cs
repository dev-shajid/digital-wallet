namespace WalletSystem.Domain.Entities;

/// <summary>
/// Extra detail for a CASH_IN (or future CASH_OUT) Transaction. Shares its primary key with
/// the Transaction it belongs to (1:1), so there is no separate Id here.
///
/// Note: the wallet system never stores real bank account numbers - that data lives only in
/// the separate Mock Bank Service. We just keep which bank and which reference id it gave us.
/// </summary>
public class BankTransfer
{
    /// <summary>Same value as the related Transaction.Id - this is both the primary key and the foreign key.</summary>
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public string BankCode { get; set; } = string.Empty;

    /// <summary>The reference/id returned by the Mock Bank Service for this operation.</summary>
    public string BankReference { get; set; } = string.Empty;
}
