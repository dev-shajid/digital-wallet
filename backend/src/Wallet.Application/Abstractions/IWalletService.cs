using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallets.Models;

namespace WalletSystem.Application.Abstractions;

/// <summary>
/// Contract for retrieving the user's primary BDT wallet details and balance.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Retrieves the default BDT wallet and current balance for the specified user.
    /// </summary>
    Task<ApiResponse<WalletBalanceResponse>> GetMyBdtWalletAsync(Guid userId, CancellationToken ct = default);
}
