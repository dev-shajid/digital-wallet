using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletApp.Models.Entities;

namespace WalletApp.Data.Configurations;

public class WalletLogConfiguration : IEntityTypeConfiguration<WalletLog>
{
    public void Configure(EntityTypeBuilder<WalletLog> builder)
    {
        builder.HasKey(wl => wl.Id);

        builder.Property(wl => wl.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(wl => wl.Amount)
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(wl => wl.BalanceBefore)
            .HasColumnType("numeric(18,4)");

        builder.Property(wl => wl.BalanceAfter)
            .HasColumnType("numeric(18,4)");

        builder.HasIndex(wl => new { wl.TransactionId, wl.WalletId })
            .IsUnique();

        builder.HasIndex(wl => new { wl.WalletId, wl.CreatedAt });

        builder.HasOne(wl => wl.Wallet)
            .WithMany(w => w.WalletLogs)
            .HasForeignKey(wl => wl.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wl => wl.Transaction)
            .WithMany(t => t.WalletLogs)
            .HasForeignKey(wl => wl.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_wallet_logs_amount_positive", "amount > 0");
            t.HasCheckConstraint(
                "ck_wallet_logs_balance_pair",
                "(balance_before IS NULL) = (balance_after IS NULL)");
            t.HasCheckConstraint(
                "ck_wallet_logs_balance_after_non_negative",
                "balance_after IS NULL OR balance_after >= 0");
        });
    }
}
