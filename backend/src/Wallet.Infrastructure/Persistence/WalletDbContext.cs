using Microsoft.EntityFrameworkCore;
using WalletSystem.Domain.Entities;

namespace WalletSystem.Infrastructure.Persistence;

/// <summary>
/// The EF Core "DbContext" is roughly what a Prisma Client or a Sequelize/TypeORM
/// data source object is in Node: it represents a connection to the database and
/// gives you one <see cref="DbSet{TEntity}"/> per table to query/insert/update through.
///
/// It does NOT contain any business rules (no balance math, no validation) - that
/// belongs in the Application/Infrastructure services built in a later phase. This
/// class is purely "how do our C# classes map to Postgres tables".
/// </summary>
public class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
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
        base.OnModelCreating(modelBuilder);

        // Loads every IEntityTypeConfiguration<T> class in this project (the files under
        // Persistence/Configurations/) instead of listing them one by one here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);

        // Backs the "AC"-prefixed account numbers generated in AccountNumberGenerator.
        modelBuilder.HasSequence<long>("account_no_seq").StartsAt(1).IncrementsBy(1);
    }
}
