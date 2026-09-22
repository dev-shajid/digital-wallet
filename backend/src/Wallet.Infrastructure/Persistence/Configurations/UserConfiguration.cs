using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(u => u.AccountNo)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Enums are stored as their string name ("USER", "ADMIN"), not as a number,
        // so the raw table data stays readable.
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.AccountNo).IsUnique();
    }
}
