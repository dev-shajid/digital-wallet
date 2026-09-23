using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wallet.Api.Common;
using Wallet.Application.Expenses;

namespace Wallet.Api.Controllers;

[ApiController]
[Authorize]
[Route("expense-categories")]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IExpenseCategoryService _service;

    public ExpenseCategoriesController(IExpenseCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await _service.GetActiveAsync(ct);
        return this.ApiOk(result);
    }
}