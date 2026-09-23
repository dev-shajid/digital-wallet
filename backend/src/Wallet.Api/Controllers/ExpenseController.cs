using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Common;
using Wallet.Application.Expenses;
using Wallet.Application.Expenses.Models;

namespace Wallet.Api.Controllers;

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
        return result.Success ? this.ApiCreated(result) : this.ApiFail(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _service.GetMyExpensesAsync(userId, ct);
        return this.ApiOk(result);
    }
}