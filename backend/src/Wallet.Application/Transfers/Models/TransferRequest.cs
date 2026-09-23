using System.ComponentModel.DataAnnotations;

namespace WalletSystem.Application.Transfers.Models;

/// <summary>
/// Payload sent by an authenticated user to transfer money to another account.
/// </summary>
public class TransferRequest
{
    [Required(ErrorMessage = "Receiver account number is required.")]
    [RegularExpression(@"^AC\d{8}$", ErrorMessage = "Enter a valid 10-character account number (e.g. AC00000001).")]
    public string ReceiverAccountNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [StringLength(255, ErrorMessage = "Note cannot exceed 255 characters.")]
    public string? Note { get; set; }
}
