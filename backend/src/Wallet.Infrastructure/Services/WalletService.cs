using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallet.Models;
using WalletSystem.Infrastructure.Persistence;
using WalletSystem.Domain.Entities;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Infrastructure.Services;

public sealed class WalletService : IWalletService
{
    private readonly WalletDbContext _dbContext;

    public WalletService(WalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<List<WalletResponse>>> GetMyWalletsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var wallets = await _dbContext.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == userId)
            .Include(wallet => wallet.Currency)
            .OrderBy(wallet => wallet.Currency.Code)
            .Select(wallet => new WalletResponse
            {
                Id = wallet.Id,
                CurrencyCode = wallet.Currency.Code,
                CurrencyName = wallet.Currency.Name,
                CurrencySymbol = wallet.Currency.Symbol,
                Balance = wallet.Balance,
                Status = wallet.Status.ToString(),
                CreatedAt = wallet.CreatedAt,
                UpdatedAt = wallet.UpdatedAt
            })
            .ToListAsync(ct);

        return new ApiResponse<List<WalletResponse>>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Wallets retrieved successfully.",
            Data = wallets
        };
    }

    public async Task<ApiResponse<CashInResponse>> CashInAsync(
        Guid userId,
        Guid walletId,
        CashInRequest request,
        CancellationToken ct = default)
    {
        if (request.Amount <= 0)
        {
            return new ApiResponse<CashInResponse>
            {
                Success = false,
                Status = StatusCodes.Status400BadRequest,
                Message = "Amount must be greater than zero.",
                Data = null
            };
        }

        await using var databaseTransaction =
            await _dbContext.Database.BeginTransactionAsync(ct);

        var wallet = await _dbContext.Wallets
            .FromSqlInterpolated($"""
                SELECT *
                FROM wallets
                WHERE id = {walletId}
                FOR UPDATE
                """)
            .Include(w => w.Currency)
            .SingleOrDefaultAsync(ct);

        if (wallet is null || wallet.UserId != userId)
        {
            return new ApiResponse<CashInResponse>
            {
                Success = false,
                Status = StatusCodes.Status404NotFound,
                Message = "Wallet not found.",
                Data = null
            };
        }

        if (wallet.Status != WalletStatus.ACTIVE)
        {
            return new ApiResponse<CashInResponse>
            {
                Success = false,
                Status = StatusCodes.Status409Conflict,
                Message = "Cash-in is not allowed for this wallet.",
                Data = null
            };
        }

        decimal balanceBefore = wallet.Balance;
        decimal balanceAfter = balanceBefore + request.Amount;

        var transaction = new Transaction
        {
            UserId = userId,
            CurrencyId = wallet.CurrencyId,
            Type = TransactionType.CASH_IN,
            Amount = request.Amount,
            Status = TransactionStatus.SUCCESS,
            Reference = $"CASHIN-{Guid.NewGuid():N}".ToUpperInvariant()
        };

        var walletLog = new WalletLog
        {
            WalletId = wallet.Id,
            Transaction = transaction,
            Direction = WalletLogDirection.CREDIT,
            Amount = request.Amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter
        };

        wallet.Balance = balanceAfter;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _dbContext.Transactions.AddAsync(transaction, ct);
        await _dbContext.WalletLogs.AddAsync(walletLog, ct);
        await _dbContext.SaveChangesAsync(ct);

        await databaseTransaction.CommitAsync(ct);

        return new ApiResponse<CashInResponse>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = "Cash-in completed successfully.",
            Data = new CashInResponse
            {
                TransactionId = transaction.Id,
                TransactionReference = transaction.Reference,
                WalletId = wallet.Id,
                Amount = request.Amount,
                CurrencyCode = wallet.Currency.Code,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                Status = transaction.Status.ToString(),
                CreatedAt = transaction.CreatedAt
            }
        };
    }
}