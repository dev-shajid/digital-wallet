namespace WalletApp.Models.Entities;

public class Expense
{
    public Guid TransactionId { get; set; }

    public Transaction Transaction { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public ExpenseCategory Category { get; set; } = null!;

    public DateOnly ExpenseDate { get; set; }
}
