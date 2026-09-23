namespace WalletSystem.Application.Transfers.Models;

/// <summary>
/// Receipt details returned to the sender after a successful P2P transfer.
/// </summary>
public class TransferResponse
{
    public Guid TransactionId { get; set; }

    public string Reference { get; set; } = string.Empty;

    public Guid CurrencyId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public string ReceiverAccountNo { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal SenderBalanceAfter { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}