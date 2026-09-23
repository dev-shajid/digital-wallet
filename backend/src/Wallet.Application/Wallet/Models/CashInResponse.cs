namespace WalletSystem.Application.Wallet.Models;

public sealed class CashInResponse
{
    public Guid TransactionId { get; init; }

    public string TransactionReference { get; init; } = string.Empty;

    public Guid WalletId { get; init; }

    public decimal Amount { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public decimal BalanceBefore { get; init; }

    public decimal BalanceAfter { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}