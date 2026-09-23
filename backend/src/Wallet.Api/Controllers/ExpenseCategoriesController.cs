using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// All expense-category endpoints live here. The class-level route is omitted on
/// purpose: each action declares its own absolute path so the two routes
/// (<c>/expense-categories</c> for users and <c>/admin/expense-categories</c> for
/// managers) can sit side by side without colliding. Authorization is per-route:
/// any authenticated user can list ACTIVE categories; only an admin can list
/// every status, create, or update.
/// </summary>
[ApiController]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IExpenseCategoryService _service;

    public ExpenseCategoriesController(IExpenseCategoryService service)
    {
        _service = service;
    }

    /// <summary>Returns ACTIVE categories only. Any authenticated user.</summary>
    [HttpGet("expense-categories")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryResponse>>>> GetActive(CancellationToken ct)
    {
        try { return Ok((await _service.GetActiveAsync(ct)).Data!); }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>Returns every category, including INACTIVE ones. Admins only.</summary>
    [HttpGet("admin/expense-categories")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryResponse>>>> GetAll(CancellationToken ct)
    {
        try { return Ok((await _service.GetAllAsync(ct)).Data!); }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>Creates a new category. New rows always start ACTIVE.</summary>
    [HttpPost("admin/expense-categories")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryResponse>>> Create(
        [FromBody] ExpenseCategoryRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(request, ct);
            return StatusCode(result.Status, result);
        }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>
    /// Updates a category's name, description, and status in one call.
    /// There is no separate status endpoint - deactivation lives here too.
    /// </summary>
    [HttpPut("admin/expense-categories/{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryResponse>>> Update(
        Guid id,
        [FromBody] ExpenseCategoryUpdateRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, ct);
            return StatusCode(result.Status, result);
        }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }
}