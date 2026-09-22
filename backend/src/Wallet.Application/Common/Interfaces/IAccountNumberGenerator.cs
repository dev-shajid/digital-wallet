namespace Wallet.Application.Common.Interfaces;

public interface IAccountNumberGenerator
{
    string Generate();

    bool Validate(string accountNumber);
}
