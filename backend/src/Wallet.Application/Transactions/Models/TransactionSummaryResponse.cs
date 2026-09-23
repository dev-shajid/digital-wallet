namespace WalletSystem.Application.Transactions.Models;

/// <summary>
/// One row in a user's transaction history, covering every <c>TransactionType</c>
/// (CASH_IN, EXPENSE today; P2P_TRANSFER, CASH_OUT once those exist). Fields that
/// only make sense for one type (like <see cref="CategoryName"/> for EXPENSE) are
/// null for every other type rather than splitting the list into separate calls.
/// </summary>
public class TransactionSummaryResponse
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;

    /// <summary>CASH_IN, CASH_OUT, P2P_TRANSFER, or EXPENSE.</summary>
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    /// <summary>DEBIT (money left this wallet) or CREDIT (money arrived), from the
    /// wallet_logs leg this transaction recorded for the current user's wallet.</summary>
    public string Direction { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? Note { get; set; }

    /// <summary>Set only for EXPENSE transactions.</summary>
    public string? CategoryName { get; set; }

    public DateTime CreatedAt { get; set; }
}
