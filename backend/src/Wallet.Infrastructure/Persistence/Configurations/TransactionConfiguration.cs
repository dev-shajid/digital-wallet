using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

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
            .HasMaxLength(50);

        builder.Property(t => t.Note)
            .HasMaxLength(500);

        builder.Property(t => t.FailureCode)
            .HasMaxLength(50);

        // No max length: raw bank/system error detail can be long; this column is
        // never shown to a normal user, only to admins debugging an issue.
        builder.Property(t => t.FailureReason);

        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Currency)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Reference).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.CreatedAt });

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("ck_transactions_amount_positive", "amount > 0");
            // A FAILED transaction must always explain why (failure_reason set);
            // any other status is free to leave it null.
            tb.HasCheckConstraint(
                "ck_transactions_failed_has_reason",
                "status <> 'FAILED' OR failure_reason IS NOT NULL");
        });
    }
}
