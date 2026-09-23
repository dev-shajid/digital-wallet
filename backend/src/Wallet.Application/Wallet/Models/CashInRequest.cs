using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Wallet.Models;

public sealed class CashInRequest
{
    [Range(
        typeof(decimal),
        "0.0001",
        "99999999999999.9999",
        ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }
}