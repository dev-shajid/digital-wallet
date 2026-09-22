using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// Fixed, hand-picked data for the one currency every fresh database must have: BDT.
/// The Id is a hardcoded GUID (not a random one) so every developer/environment/test
/// run ends up with the exact same row - useful for writing tests that reference "the
/// BDT currency" by a known id instead of having to look it up first.
/// </summary>
public static class CurrencySeed
{
    public static readonly Guid BdtId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Currency Bdt => new()
    {
        Id = BdtId,
        Code = "BDT",
        Name = "Bangladeshi Taka",
        Symbol = "৳",
        DecimalPlaces = 2,
        Status = CurrencyStatus.ACTIVE,
        // EF Core's HasData seeding requires fixed values for every non-nullable
        // property (it can't use the C# default-value initializers on the class),
        // so CreatedAt/UpdatedAt are set explicitly here instead of relying on
        // AuditableEntity's `= DateTime.UtcNow` defaults.
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
