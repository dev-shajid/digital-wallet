using Wallet.Application.Common.Interfaces;

namespace Wallet.Infrastructure.Security;

public class AccountNumberGenerator : IAccountNumberGenerator
{
    public const string Prefix = "AC";
    public const long DefaultStartSequence = 100000001;

    private readonly IAppDbContext? _dbContext;

    public AccountNumberGenerator()
    {
    }

    public AccountNumberGenerator(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext == null)
        {
            throw new InvalidOperationException("IAppDbContext is required to generate sequential account numbers.");
        }

        var seq = await _dbContext.NextAccountSequenceValueAsync(cancellationToken);
        return FormatFromSequence(seq);
    }

    public string FormatFromSequence(long sequenceValue)
    {
        if (sequenceValue < 1 || sequenceValue > 999999999)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceValue), "Sequence value must fit within 9 digits.");
        }

        var baseString = sequenceValue.ToString("D9");
        var digits = new int[10];
        for (var i = 0; i < 9; i++)
        {
            digits[i] = baseString[i] - '0';
        }

        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var val = digits[i];
            // Even indexes from left (0, 2, 4, 6, 8) correspond to even positions from right in a 10-digit number
            if (i % 2 == 0)
            {
                val *= 2;
                if (val > 9)
                {
                    val -= 9;
                }
            }

            sum += val;
        }

        digits[9] = (10 - (sum % 10)) % 10;

        return Prefix + string.Concat(digits);
    }

    public bool Validate(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) ||
            accountNumber.Length != Prefix.Length + 10 ||
            !accountNumber.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var numericPart = accountNumber[Prefix.Length..];
        if (!numericPart.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            var val = numericPart[i] - '0';
            if (i % 2 == 0)
            {
                val *= 2;
                if (val > 9)
                {
                    val -= 9;
                }
            }

            sum += val;
        }

        return sum % 10 == 0;
    }
}
