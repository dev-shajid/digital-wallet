using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);

        // numeric(18,4): fixed-point decimal, never a float, so money never loses precision.
        builder.Property(w => w.Balance)
            .HasColumnType("numeric(18,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(w => w.User)
            .WithMany(u => u.Wallets)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Currency)
            .WithMany(c => c.Wallets)
            .HasForeignKey(w => w.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // At most one wallet per (user, currency) pair.
        builder.HasIndex(w => new { w.UserId, w.CurrencyId }).IsUnique();

        builder.ToTable(tb => tb.HasCheckConstraint("ck_wallets_balance_non_negative", "balance >= 0"));
    }
}
