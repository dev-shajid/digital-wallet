namespace WalletSystem.Application.Transfers.Models;

/// <summary>
/// Receipt details returned to the sender upon a successful P2P money transfer.
/// </summary>
public class TransferResponse
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ReceiverAccountNo { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SenderBalanceAfter { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., "SUCCESS"
    public DateTime CreatedAt { get; set; }
}
