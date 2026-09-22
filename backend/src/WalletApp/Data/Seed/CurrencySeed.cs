using WalletApp.Models.Entities;
using WalletApp.Models.Enums;

namespace WalletApp.Data.Seed;

internal static class CurrencySeed
{
    internal static readonly Guid BdtId = new("11111111-1111-4111-8111-111111111111");

    internal static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    internal static Currency Bdt => new()
    {
        Id = BdtId,
        Code = "BDT",
        Name = "Bangladeshi Taka",
        Symbol = "৳",
        CountryCode = "BD",
        DecimalPlaces = 2,
        Status = CurrencyStatus.ACTIVE,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };
}
