using System.Security.Cryptography;
using Wallet.Application.Common.Interfaces;

namespace Wallet.Infrastructure.Security;

public class AccountNumberGenerator : IAccountNumberGenerator
{
    public const string Prefix = "AC";

    public string Generate()
    {
        var digits = new int[10];

        // First digit non-zero (1-9)
        digits[0] = RandomNumberGenerator.GetInt32(1, 10);

        // Digits 1 to 8 (0-9)
        for (var i = 1; i < 9; i++)
        {
            digits[i] = RandomNumberGenerator.GetInt32(0, 10);
        }

        // Calculate Luhn check digit for position 9 (10th digit)
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
