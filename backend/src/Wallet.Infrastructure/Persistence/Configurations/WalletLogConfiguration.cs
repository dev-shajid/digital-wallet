using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class WalletLogConfiguration : IEntityTypeConfiguration<WalletLog>
{
    public void Configure(EntityTypeBuilder<WalletLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(l => l.Amount)
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(l => l.BalanceBefore)
            .HasColumnType("numeric(18,4)");

        builder.Property(l => l.BalanceAfter)
            .HasColumnType("numeric(18,4)");

        builder.HasOne(l => l.Wallet)
            .WithMany(w => w.WalletLogs)
            .HasForeignKey(l => l.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Transaction)
            .WithMany(t => t.WalletLogs)
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // A wallet can only appear once per transaction (e.g. one DEBIT row for the
        // sender in a P2P transfer, not two).
        builder.HasIndex(l => new { l.TransactionId, l.WalletId }).IsUnique();

        // Powers "give me this wallet's statement, newest first".
        builder.HasIndex(l => new { l.WalletId, l.CreatedAt });

        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("ck_wallet_logs_amount_positive", "amount > 0");
            // Both balance snapshots are set together (when the leg is applied) or both left null (still pending).
            tb.HasCheckConstraint(
                "ck_wallet_logs_balance_pair",
                "(balance_before IS NULL) = (balance_after IS NULL)");
            tb.HasCheckConstraint(
                "ck_wallet_logs_balance_after_non_negative",
                "balance_after IS NULL OR balance_after >= 0");
        });
    }
}
