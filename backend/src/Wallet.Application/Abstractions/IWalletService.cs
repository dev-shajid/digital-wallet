using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallet.Models;

namespace WalletSystem.Application.Abstractions;

public interface IWalletService
{
    Task<ApiResponse<List<WalletResponse>>> GetMyWalletsAsync(
        Guid userId,
        CancellationToken ct = default);
}