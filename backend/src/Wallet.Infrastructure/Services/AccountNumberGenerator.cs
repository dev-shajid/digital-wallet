using System.Security.Cryptography;
using System.Text;
using WalletSystem.Application.Abstractions;

namespace WalletSystem.Infrastructure.Services;

/// <summary>
/// Generates 10-digit account numbers where the 10th digit is a Luhn algorithm check digit.
/// </summary>
public class AccountNumberGenerator : IAccountNumberGenerator
{
    public string GenerateAccountNumber()
    {
        // Step 1: Generate 9 random digits (first digit 1-9 to avoid leading zero)
        var sb = new StringBuilder(10);
        sb.Append(RandomNumberGenerator.GetInt32(1, 10)); // First digit: 1-9

        for (int i = 0; i < 8; i++)
        {
            sb.Append(RandomNumberGenerator.GetInt32(0, 10)); // Next 8 digits: 0-9
        }

        string base9Digits = sb.ToString();

        // Step 2: Compute Luhn check digit
        int checkDigit = CalculateLuhnCheckDigit(base9Digits);
        sb.Append(checkDigit);

        return sb.ToString();
    }

    public bool ValidateAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length != 10 || !accountNumber.All(char.IsDigit))
            return false;

        string base9Digits = accountNumber[..9];
        int expectedCheckDigit = CalculateLuhnCheckDigit(base9Digits);
        int actualCheckDigit = accountNumber[9] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    private static int CalculateLuhnCheckDigit(string digits)
    {
        int sum = 0;

        // Double every second digit from right to left
        for (int i = 0; i < digits.Length; i++)
        {
            int digit = digits[digits.Length - 1 - i] - '0';

            if (i % 2 == 0)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
        }

        return (10 - (sum % 10)) % 10;
    }
}
