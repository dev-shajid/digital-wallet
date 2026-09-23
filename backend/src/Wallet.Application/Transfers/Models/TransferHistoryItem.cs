namespace WalletSystem.Application.Transfers.Models;

/// <summary>
/// A single entry in the user's P2P transfer history (either money sent or money received).
/// </summary>
public class TransferHistoryItem
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // "SENT" or "RECEIVED"
    public string CounterpartyAccountNo { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty; // e.g., "SUCCESS"
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
