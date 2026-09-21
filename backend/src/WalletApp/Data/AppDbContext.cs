using Microsoft.EntityFrameworkCore;
using WalletApp.Models.Entities;

namespace WalletApp.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Currency> Currencies => Set<Currency>();

    public DbSet<Wallet> Wallets => Set<Wallet>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<WalletLog> WalletLogs => Set<WalletLog>();

    public DbSet<BankTransfer> BankTransfers => Set<BankTransfer>();

    public DbSet<P2PTransfer> P2PTransfers => Set<P2PTransfer>();

    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = utcNow;
                }

                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    auditable.UpdatedAt = utcNow;
                }
            }

            if (entry.Entity is Currency currency && entry.State is EntityState.Added or EntityState.Modified)
            {
                if (entry.State == EntityState.Added)
                {
                    currency.CreatedAt = utcNow;
                }

                currency.UpdatedAt = utcNow;
            }

            if (entry.Entity is WalletLog walletLog && entry.State == EntityState.Added)
            {
                walletLog.CreatedAt = utcNow;
            }
        }
    }
}
