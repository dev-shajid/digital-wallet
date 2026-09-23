using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;

namespace WalletSystem.Application.Abstractions;

/// <summary>
/// Expense categories. New rows always start ACTIVE; "removing" means flipping a
/// row to INACTIVE through the update endpoint - the database's
/// ON DELETE RESTRICT on expenses.category_id prevents hard deletes anyway.
/// </summary>
public interface IExpenseCategoryService
{
    /// <summary>Role-aware list: admins see every status, regular users see only ACTIVE.</summary>
    Task<ApiResponse<List<ExpenseCategoryResponse>>> GetForCallerAsync(bool isAdmin, CancellationToken ct = default);

    /// <summary>Creates a new category. New rows always start ACTIVE - clients cannot pick the status on create.</summary>
    Task<ApiResponse<ExpenseCategoryResponse>> CreateAsync(ExpenseCategoryRequest request, CancellationToken ct = default);

    /// <summary>Updates name, description, and status of an existing category in one call.</summary>
    Task<ApiResponse<ExpenseCategoryResponse>> UpdateAsync(Guid id, ExpenseCategoryUpdateRequest request, CancellationToken ct = default);
}
