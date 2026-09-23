namespace WalletSystem.Application.Transfers.Models;

/// <summary>
/// Public preview information for an account lookup before sending money.
/// </summary>
public class AccountLookupResponse
{
    public string AccountNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
