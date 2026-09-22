namespace WalletSystem.Domain.Entities;

/// <summary>
/// Extra detail for an EXPENSE Transaction (1:1, keyed by TransactionId). The amount and any
/// user-entered description already live on the parent Transaction (Amount, Note); this table
/// only adds the category and the date the expense happened on.
/// </summary>
public class Expense
{
    public Guid TransactionId { get; set; }
    public Transaction Transaction { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;

    public DateOnly ExpenseDate { get; set; }
}
