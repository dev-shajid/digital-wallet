using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Entities;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class P2PTransferConfiguration : IEntityTypeConfiguration<P2PTransfer>
{
    public void Configure(EntityTypeBuilder<P2PTransfer> builder)
    {
        builder.HasKey(p => p.TransactionId);

        builder.HasIndex(p => p.ReceiverWalletId);

        builder.HasOne(p => p.Transaction)
            .WithOne(t => t.P2PTransfer)
            .HasForeignKey<P2PTransfer>(p => p.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.ReceiverWallet)
            .WithMany(w => w.ReceivedP2PTransfers)
            .HasForeignKey(p => p.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
