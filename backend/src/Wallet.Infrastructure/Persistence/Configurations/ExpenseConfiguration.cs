using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.TransactionId);

        builder.Property(e => e.ExpenseDate)
            .HasColumnType("date")
            .IsRequired();

        builder.HasOne(e => e.Transaction)
            .WithOne(t => t.Expense)
            .HasForeignKey<Expense>(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // A category referenced by any expense can never be deleted, only set INACTIVE
        // (enforced here at the database level, and again in application logic).
        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
