using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Expenses;
using WalletSystem.Application.Expenses.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Api.Controllers;

// Admins don't have a personal wallet to spend from - same restriction as
// WalletsController.
[ApiController]
[Authorize(Roles = nameof(Role.USER))]
[Route("expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;

    public ExpensesController(IExpenseService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _service.CreateExpenseAsync(userId, request, ct);
        return StatusCode(result.Status, result);
    }
}