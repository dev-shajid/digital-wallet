namespace WalletSystem.Application.ExpenseCategories.Models;

/// <summary>One category in API responses. <c>Status</c> is its string name (e.g. "ACTIVE").</summary>
public class ExpenseCategoryResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
