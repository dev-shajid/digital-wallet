using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Abstractions;
using WalletSystem.Infrastructure.Persistence;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Generates account numbers as "AC" followed by an 8-digit, zero-padded value taken from
/// the "account_no_seq" Postgres sequence, e.g. AC00000001. The sequence guarantees
/// uniqueness under concurrent registrations without a generate-and-check retry loop.
/// </summary>
public class AccountNumberGenerator : IAccountNumberGenerator
{
    private const string Prefix = "AC";
    private const int DigitCount = 8;

    private readonly WalletDbContext _dbContext;

    public AccountNumberGenerator(WalletDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAccountNumberAsync(CancellationToken ct = default)
    {
        long next = await _dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('account_no_seq') AS \"Value\"")
            .SingleAsync(ct);

        return Prefix + next.ToString().PadLeft(DigitCount, '0');
    }

    public bool ValidateAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length != Prefix.Length + DigitCount)
            return false;

        return accountNumber.StartsWith(Prefix, StringComparison.Ordinal)
            && accountNumber[Prefix.Length..].All(char.IsDigit);
    }
}
