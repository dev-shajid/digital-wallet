using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transfers.Models;

namespace WalletSystem.Application.Abstractions;

/// <summary>
/// Contract for P2P money transfers and transfer history operations.
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Executes an atomic peer-to-peer money transfer between the authenticated sender and the recipient account.
    /// </summary>
    Task<ApiResponse<TransferResponse>> TransferAsync(Guid senderUserId, TransferRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the transfer history (both sent and received) for the specified user.
    /// </summary>
    Task<ApiResponse<List<TransferHistoryItem>>> GetHistoryAsync(Guid userId, CancellationToken ct = default);
}
