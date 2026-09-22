namespace WalletSystem.Application.Abstractions;

public interface IAccountNumberGenerator
{
    string GenerateAccountNumber();
    bool ValidateAccountNumber(string accountNumber);
}
