using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wallet.Domain.Entities;

namespace Wallet.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Currency> Currencies { get; }

    DbSet<Domain.Entities.Wallet> Wallets { get; }

    DbSet<Transaction> Transactions { get; }

    DbSet<WalletLog> WalletLogs { get; }

    DbSet<BankTransfer> BankTransfers { get; }

    DbSet<P2PTransfer> P2PTransfers { get; }

    DbSet<ExpenseCategory> ExpenseCategories { get; }

    DbSet<Expense> Expenses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
