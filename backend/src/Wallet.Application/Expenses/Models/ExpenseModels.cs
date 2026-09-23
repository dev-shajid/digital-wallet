namespace WalletSystem.Application.Expenses.Models;

public class ExpenseCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ExpenseCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // ACTIVE / INACTIVE
}

public class UpdateCategoryStatusRequest
{
    public string Status { get; set; } = string.Empty; // "ACTIVE" | "INACTIVE"
}

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