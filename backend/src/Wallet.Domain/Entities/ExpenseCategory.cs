using WalletSystem.Domain.Common;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Domain.Entities;

/// <summary>
/// An admin-managed category a user picks when recording an Expense (e.g. "Food", "Transport").
/// Categories are never hard-deleted once an expense uses them (ON DELETE RESTRICT) - set
/// Status to INACTIVE instead, which blocks new expenses but keeps old ones visible.
/// </summary>
public class ExpenseCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public CategoryStatus Status { get; set; } = CategoryStatus.ACTIVE;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
