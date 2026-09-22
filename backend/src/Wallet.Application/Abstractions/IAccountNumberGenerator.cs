namespace WalletSystem.Application.Abstractions;

public interface IAccountNumberGenerator
{
    Task<string> GenerateAccountNumberAsync(CancellationToken ct = default);
    bool ValidateAccountNumber(string accountNumber);
}
