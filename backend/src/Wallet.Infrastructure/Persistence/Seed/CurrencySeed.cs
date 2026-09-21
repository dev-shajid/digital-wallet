using Wallet.Domain.Entities;
using Wallet.Domain.Enums;

namespace Wallet.Infrastructure.Persistence.Seed;

internal static class CurrencySeed
{
    internal static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static Currency Bdt => new()
    {
        Code = "BDT",
        Name = "Bangladeshi Taka",
        Symbol = "৳",
        IconUrl = "",
        DecimalPlaces = 2,
        Status = CurrencyStatus.ACTIVE,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };
}
