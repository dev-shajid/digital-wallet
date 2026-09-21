using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletApp.Models.Entities;

namespace WalletApp.Data.Configurations;

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
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
