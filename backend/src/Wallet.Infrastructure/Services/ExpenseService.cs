using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Expenses;
using WalletSystem.Application.Expenses.Models;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Services;

public class ExpenseService : IExpenseService
{
    private readonly WalletDbContext _dbContext;

    public ExpenseService(WalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<ExpenseResponse>> CreateExpenseAsync(Guid userId, CreateExpenseRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
        {
            return Fail(400, "Amount must be greater than zero.");
        }

        if (request.CurrencyId == Guid.Empty)
        {
            return Fail(400, "Currency is required.");
        }

        var wallet = await _dbContext.Wallets
            .Include(w => w.Currency)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.CurrencyId == request.CurrencyId, ct);
        if (wallet is null)
        {
            return Fail(404, "You don't have a wallet in that currency.");
        }

        if (wallet.Status != WalletStatus.ACTIVE)
        {
            return Fail(409, "Wallet is not active.");
        }

        var category = await _dbContext.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
        {
            return Fail(404, "Category not found.");
        }

        if (category.Status != CategoryStatus.ACTIVE)
        {
            return Fail(409, "This category is no longer available.");
        }

        if (wallet.Balance < request.Amount)
        {
            return Fail(422, "Insufficient wallet balance.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            // Lock the wallet row so two concurrent requests can't read the same stale balance.
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT id FROM wallets WHERE id = {wallet.Id} FOR UPDATE", ct);

            // Re-read after acquiring the lock.
            var lockedWallet = await _dbContext.Wallets
                .Include(w => w.Currency)
                .FirstAsync(w => w.Id == wallet.Id, ct);

            if (lockedWallet.Status != WalletStatus.ACTIVE)
            {
                await transaction.RollbackAsync(ct);
                return Fail(409, "Wallet is not active.");
            }

            if (lockedWallet.Balance < request.Amount)
            {
                await transaction.RollbackAsync(ct);
                return Fail(422, "Insufficient wallet balance.");
            }

            var reference = "TXN" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            var balanceBefore = lockedWallet.Balance;
            var balanceAfter = balanceBefore - request.Amount;

            var txnEntity = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CurrencyId = lockedWallet.CurrencyId,
                Type = TransactionType.EXPENSE,
                Amount = request.Amount,
                Status = TransactionStatus.SUCCESS,
                Reference = reference,
                Note = request.Note,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Transactions.Add(txnEntity);

            var walletLog = new WalletLog
            {
                Id = Guid.NewGuid(),
                WalletId = lockedWallet.Id,
                TransactionId = txnEntity.Id,
                Direction = WalletLogDirection.DEBIT,
                Amount = request.Amount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.WalletLogs.Add(walletLog);

            lockedWallet.Balance = balanceAfter;

            var expenseDate = request.ExpenseDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var expense = new Expense
            {
                TransactionId = txnEntity.Id,
                CategoryId = category.Id,
                ExpenseDate = expenseDate
            };
            _dbContext.Expenses.Add(expense);

            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new ApiResponse<ExpenseResponse>
            {
                Success = true,
                Status = 201,
                Message = "Expense recorded successfully.",
                Data = new ExpenseResponse
                {
                    TransactionId = txnEntity.Id,
                    Reference = txnEntity.Reference,
                    CategoryName = category.Name,
                    Amount = txnEntity.Amount,
                    CurrencyCode = lockedWallet.Currency.Code,
                    Note = txnEntity.Note,
                    ExpenseDate = expenseDate,
                    WalletBalanceAfter = balanceAfter,
                    CreatedAt = txnEntity.CreatedAt
                }
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static ApiResponse<ExpenseResponse> Fail(int status, string message) => new()
    {
        Success = false,
        Status = status,
        Message = message
    };
}