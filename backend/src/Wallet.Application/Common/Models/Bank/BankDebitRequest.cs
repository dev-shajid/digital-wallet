namespace Wallet.Application.Common.Models.Bank;

public record BankDebitRequest(
    string BankCode,
    string AccountNumber,
    decimal Amount,
    string CurrencyCode,
    string? Reference = null);
