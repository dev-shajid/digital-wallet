using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Application.Abstractions;

/// <summary>
/// Admin-managed expense categories. New categories start ACTIVE; "deleting" means
/// setting INACTIVE. Hard delete is intentionally not supported - the database's
/// ON DELETE RESTRICT on expenses.category_id is the safety net.
/// </summary>
public interface IExpenseCategoryService
{
    /// <summary>Admin: every category, regardless of status. Ordered by name.</summary>
    Task<ApiResponse<List<ExpenseCategoryResponse>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Any authenticated user: ACTIVE categories only. Ordered by name.</summary>
    Task<ApiResponse<List<ExpenseCategoryResponse>>> GetActiveAsync(CancellationToken ct = default);

    Task<ApiResponse<ExpenseCategoryResponse>> CreateAsync(ExpenseCategoryRequest request, CancellationToken ct = default);

    Task<ApiResponse<ExpenseCategoryResponse>> UpdateAsync(Guid id, ExpenseCategoryRequest request, CancellationToken ct = default);

    Task<ApiResponse<ExpenseCategoryResponse>> SetStatusAsync(Guid id, CategoryStatus status, CancellationToken ct = default);
}
