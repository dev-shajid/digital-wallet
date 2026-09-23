using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;

namespace WalletSystem.Api.Controllers;

/// <summary>User-facing read-only endpoint for picking a category when recording an expense.</summary>
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

    /// <summary>Returns ACTIVE categories only. Any authenticated user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryResponse>>>> GetActive(
        CancellationToken ct)
    {
        try
        {
            return Ok((await _service.GetActiveAsync(ct)).Data!);
        }
        catch (DomainException ex)
        {
            return ex.ToActionResult(this);
        }
    }
}