using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class P2PTransferConfiguration : IEntityTypeConfiguration<P2PTransfer>
{
    public void Configure(EntityTypeBuilder<P2PTransfer> builder)
    {
        builder.HasKey(p => p.TransactionId);

        builder.HasOne(p => p.Transaction)
            .WithOne(t => t.P2PTransfer)
            .HasForeignKey<P2PTransfer>(p => p.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ReceiverWallet)
            .WithMany(w => w.IncomingP2PTransfers)
            .HasForeignKey(p => p.ReceiverWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Speeds up "list transfers I received" (looked up by receiver wallet).
        builder.HasIndex(p => p.ReceiverWalletId);
    }
}
