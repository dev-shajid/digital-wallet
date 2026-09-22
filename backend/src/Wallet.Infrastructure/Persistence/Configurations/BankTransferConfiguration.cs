using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class BankTransferConfiguration : IEntityTypeConfiguration<BankTransfer>
{
    public void Configure(EntityTypeBuilder<BankTransfer> builder)
    {
        // 1:1 with Transaction: this table's primary key IS the transaction's id,
        // there is no separate identity column.
        builder.HasKey(b => b.TransactionId);

        builder.Property(b => b.BankCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.BankReference)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(b => b.Transaction)
            .WithOne(t => t.BankTransfer)
            .HasForeignKey<BankTransfer>(b => b.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
