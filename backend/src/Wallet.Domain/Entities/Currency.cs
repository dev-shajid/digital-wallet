using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class Currency : AuditableEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Symbol { get; set; }

    public required string CountryCode { get; set; }

    public int DecimalPlaces { get; set; }

    public CurrencyStatus Status { get; set; }

    public ICollection<Wallet> Wallets { get; set; } = [];

    public ICollection<Transaction> Transactions { get; set; } = [];
}
