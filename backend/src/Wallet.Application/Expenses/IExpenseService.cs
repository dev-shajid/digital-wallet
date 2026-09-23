using Wallet.Application.Common.Models;
using Wallet.Application.Expenses.Models;

namespace Wallet.Application.Expenses;

public interface IExpenseService
{
    Task<ApiResponse<ExpenseResponse>> CreateExpenseAsync(Guid userId, CreateExpenseRequest request, CancellationToken ct = default);
    Task<ApiResponse<List<ExpenseResponse>>> GetMyExpensesAsync(Guid userId, CancellationToken ct = default);
}