using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transactions.Models;

namespace WalletSystem.Application.Transactions;

public interface ITransactionService
{
    /// <summary>Every transaction the given user initiated (cash-in, expenses, and -
    /// once built - P2P transfers), most recent first.</summary>
    Task<ApiResponse<List<TransactionSummaryResponse>>> GetMyTransactionsAsync(
        Guid userId,
        CancellationToken ct = default);
}
