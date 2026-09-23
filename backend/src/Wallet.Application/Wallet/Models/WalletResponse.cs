namespace WalletSystem.Application.Wallet.Models;

public sealed class WalletResponse
{
    public Guid Id { get; init; }

    public Guid CurrencyId { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public string CurrencyName { get; init; } = string.Empty;

    public string CurrencySymbol { get; init; } = string.Empty;

    public decimal Balance { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}