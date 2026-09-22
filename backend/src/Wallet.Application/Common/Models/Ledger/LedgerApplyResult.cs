namespace Wallet.Application.Common.Models.Ledger;

public record LedgerApplyResult(bool Success, string? FailureCode = null, string? FailureReason = null)
{
    public static LedgerApplyResult Succeeded() => new(true);

    public static LedgerApplyResult Failed(string failureCode, string failureReason) =>
        new(false, failureCode, failureReason);
}
