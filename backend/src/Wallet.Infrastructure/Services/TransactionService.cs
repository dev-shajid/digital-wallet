using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transactions;
using WalletSystem.Application.Transactions.Models;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly WalletDbContext _dbContext;

    public TransactionService(WalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<TransactionSummaryResponse>>> GetMyTransactionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new TransactionSummaryResponse
            {
                TransactionId = t.Id,
                Reference = t.Reference,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Direction = t.WalletLogs
                    .Select(log => log.Direction.ToString())
                    .FirstOrDefault() ?? string.Empty,
                Amount = t.Amount,
                CurrencyCode = t.Currency.Code,
                Note = t.Note,
                CategoryName = t.Expense != null ? t.Expense.Category.Name : null,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return new ApiResponse<List<TransactionSummaryResponse>>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Transactions retrieved successfully.",
            Data = transactions
        };
    }
}
