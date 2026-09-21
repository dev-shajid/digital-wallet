using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class BankTransferConfiguration : IEntityTypeConfiguration<BankTransfer>
{
    public void Configure(EntityTypeBuilder<BankTransfer> builder)
    {
        builder.HasKey(b => b.TransactionId);

        builder.Property(b => b.BankCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.BankReference)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(b => b.Transaction)
            .WithOne(t => t.BankTransfer)
            .HasForeignKey<BankTransfer>(b => b.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
