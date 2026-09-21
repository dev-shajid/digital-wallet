using WalletApp.Models.Enums;

namespace WalletApp.Models.Entities;

public class ExpenseCategory : AuditableEntity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public CategoryStatus Status { get; set; }

    public ICollection<Expense> Expenses { get; set; } = [];
}
