using Wallet.Application.Common.Models.Ledger;
using Wallet.Domain.Entities;
using Wallet.Domain.Enums;

namespace Wallet.Application.Common.Interfaces;

public interface IWalletLedger
{
    Task<Transaction> CreatePendingAsync(
        Guid userId,
        Guid currencyId,
        TransactionType type,
        decimal amount,
        string? note,
        IReadOnlyList<WalletLegDto> legs,
        CancellationToken ct = default);

    Task<LedgerApplyResult> ApplyLegAsync(Guid walletLogId, CancellationToken ct = default);

    Task MarkSuccessAsync(Guid transactionId, CancellationToken ct = default);

    Task MarkFailedAsync(Guid transactionId, string failureCode, string failureReason, CancellationToken ct = default);
}
