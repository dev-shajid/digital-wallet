using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Expenses.Models;

namespace WalletSystem.Application.Expenses;

public interface IExpenseService
{
    Task<ApiResponse<ExpenseResponse>> CreateExpenseAsync(Guid userId, CreateExpenseRequest request, CancellationToken ct = default);
}