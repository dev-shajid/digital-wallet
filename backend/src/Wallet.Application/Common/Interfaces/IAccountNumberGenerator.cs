namespace Wallet.Application.Common.Interfaces;

public interface IAccountNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);

    string FormatFromSequence(long sequenceValue);

    bool Validate(string accountNumber);
}
