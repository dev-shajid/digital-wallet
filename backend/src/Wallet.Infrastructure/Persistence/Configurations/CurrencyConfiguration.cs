using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;
using WalletSystem.Infrastructure.Persistence.Seed;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();

        // Seeds the BDT row with a fixed id, so every environment/migration ends up
        // with the exact same currency row (needed for repeatable tests/migrations).
        builder.HasData(CurrencySeed.Bdt);
    }
}
