using WalletSystem.Domain.Common;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>An ISO 4217 currency (e.g. BDT, USD). BDT is seeded with a fixed id so migrations/tests are deterministic.</summary>
public class Currency : AuditableEntity
{
    /// <summary>ISO 4217 code, e.g. "BDT". Unique and immutable.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public int DecimalPlaces { get; set; }

    public CurrencyStatus Status { get; set; } = CurrencyStatus.ACTIVE;

    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
