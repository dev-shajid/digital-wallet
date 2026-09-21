using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class Currency
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Symbol { get; set; }

    public required string IconUrl { get; set; }

    public int DecimalPlaces { get; set; }

    public CurrencyStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Wallet> Wallets { get; set; } = [];

    public ICollection<Transaction> Transactions { get; set; } = [];
}
