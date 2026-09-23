using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WalletSystem.Application.Abstractions;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Services;

namespace WalletSystem.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-time admin seeder. Runs once at startup; if no admin user exists
/// yet, creates one with a fixed email and password so the team can hit the
/// /admin/... endpoints from Swagger without hand-editing the database.
///
/// Not safe for production - this is gated behind an opt-in configuration flag
/// (see <see cref="ShouldSeed"/>). Defaults to "off" unless <c>Admin:Seed=true</c>
/// is set, so a production deployment won't accidentally create a privileged user.
/// </summary>
public static class AdminSeed
{
    /// <summary>Fixed GUID for the seed admin so it's deterministic across runs.</summary>
    public static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public const string SeedEmail = "admin@wallet.local";
    public const string SeedPassword = "Admin@123";
    public const string SeedName = "Seed Admin";

    /// <summary>Returns true only if the host has explicitly opted in.</summary>
    public static bool ShouldSeed(IConfiguration configuration)
        => string.Equals(configuration["Admin:Seed"], "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Idempotent: inserts the seed admin + matching BDT wallet only if no admin
    /// row exists yet. Hashes the password with the same PBKDF2 settings the
    /// regular registration uses, so the user can sign in immediately.
    /// </summary>
    public static async Task EnsureSeededAsync(WalletDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        var adminExists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Role == Role.ADMIN, ct);

        if (adminExists)
        {
            return;
        }

        var admin = new User
        {
            Id = AdminId,
            Name = SeedName,
            Email = SeedEmail.ToLowerInvariant(),
            // AccountNo is allocated from the sequence inside AccountNumberGenerator; the
            // user/wallet pair will fail to insert without one, so generate it here.
            AccountNo = await new AccountNumberGenerator(db).GenerateAccountNumberAsync(ct),
            PasswordHash = hasher.HashPassword(SeedPassword),
            Role = Role.ADMIN,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = admin.Id,
            CurrencyId = CurrencySeed.BdtId,
            Balance = 0m,
            Status = WalletStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync(ct);
    }
}
