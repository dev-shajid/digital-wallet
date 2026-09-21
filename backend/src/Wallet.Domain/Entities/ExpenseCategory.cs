using Wallet.Domain.Common;
using Wallet.Domain.Enums;

namespace Wallet.Domain.Entities;

public class ExpenseCategory : AuditableEntity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public CategoryStatus Status { get; set; }

    public ICollection<Expense> Expenses { get; set; } = [];
}
