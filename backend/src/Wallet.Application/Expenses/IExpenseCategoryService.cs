using WalletSystem.Application.Expenses.Models;
using WalletSystem.Application.Common.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Application.Expenses;

public interface IExpenseCategoryService
{
    Task<ApiResponse<List<ExpenseCategoryResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<ApiResponse<List<ExpenseCategoryResponse>>> GetActiveAsync(CancellationToken ct = default);
    Task<ApiResponse<ExpenseCategoryResponse>> CreateAsync(ExpenseCategoryRequest request, CancellationToken ct = default);
    Task<ApiResponse<ExpenseCategoryResponse>> UpdateAsync(Guid id, ExpenseCategoryRequest request, CancellationToken ct = default);
    Task<ApiResponse<ExpenseCategoryResponse>> SetStatusAsync(Guid id, CategoryStatus status, CancellationToken ct = default);
}
