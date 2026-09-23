namespace WalletSystem.Application.Expenses.Models;

public class CreateExpenseRequest
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateOnly? ExpenseDate { get; set; }
}

public class ExpenseResponse
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal WalletBalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}