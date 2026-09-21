using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Reference)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Reference)
            .IsUnique();

        builder.Property(t => t.Note)
            .HasMaxLength(1000);

        builder.Property(t => t.FailureCode)
            .HasMaxLength(50);

        builder.Property(t => t.FailureReason)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => new { t.WalletId, t.CurrencyCode })
            .HasPrincipalKey(w => new { w.Id, w.CurrencyCode })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
            t.HasCheckConstraint(
                "ck_transactions_failed_requires_reason",
                "status <> 'FAILED' OR failure_reason IS NOT NULL");
        });
    }
}
