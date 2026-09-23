using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallet.Models;
using WalletSystem.Infrastructure.Persistence;

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
}