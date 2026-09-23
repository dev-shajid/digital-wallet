using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Expenses;
using WalletSystem.Application.Expenses.Models;

namespace WalletSystem.Api.Controllers;

[ApiController]
[Authorize]
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

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _service.GetMyExpensesAsync(userId, ct);
        return StatusCode(result.Status, result);
    }
}