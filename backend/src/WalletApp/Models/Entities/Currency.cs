using WalletApp.Models.Enums;

namespace WalletApp.Models.Entities;

public class Currency : AuditableEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Symbol { get; set; }

    public required string IconUrl { get; set; }

    public int DecimalPlaces { get; set; }

    public CurrencyStatus Status { get; set; }

    public ICollection<Wallet> Wallets { get; set; } = [];

    public ICollection<Transaction> Transactions { get; set; } = [];
}
