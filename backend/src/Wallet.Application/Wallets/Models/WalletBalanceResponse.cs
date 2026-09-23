namespace WalletSystem.Application.Wallets.Models;

/// <summary>
/// Summary of a user's wallet balance and currency details.
/// </summary>
public class WalletBalanceResponse
{
    public Guid WalletId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty; // e.g. "BDT"
    public string CurrencySymbol { get; set; } = string.Empty; // e.g. "৳"
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty; // e.g. "ACTIVE"
}
