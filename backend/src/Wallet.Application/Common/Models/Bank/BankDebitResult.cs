namespace Wallet.Application.Common.Models.Bank;

public record BankDebitResult(
    bool Success,
    string? BankReference = null,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static BankDebitResult Succeeded(string bankReference) =>
        new(true, BankReference: bankReference);

    public static BankDebitResult Failed(string errorCode, string errorMessage) =>
        new(false, ErrorCode: errorCode, ErrorMessage: errorMessage);
}
