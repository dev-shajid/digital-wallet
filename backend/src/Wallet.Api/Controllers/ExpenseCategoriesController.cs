using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// Expense-category endpoints. The URL has no "admin" segment - whether a
/// caller gets the full list or just the ACTIVE subset is decided by their
/// JWT role, not their path. Posting and updating are reserved for admins
/// through <see cref="AuthorizeAttribute"/>; the GET serves both roles but
/// returns a different result depending on who is calling.
/// </summary>
[ApiController]
[Route("expense-categories")]
public class ExpenseCategoriesController : ControllerBase
{
    private readonly IExpenseCategoryService _service;

    public ExpenseCategoriesController(IExpenseCategoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists categories. Returns ACTIVE only to a regular user, every status
    /// to an admin - same endpoint, role-aware payload.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryResponse>>>> List(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetForCallerAsync(isAdmin: User.IsInRole("ADMIN"), ct);
            return StatusCode(result.Status, result);
        }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>Creates a new category. New rows always start ACTIVE.</summary>
    [HttpPost]
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
    [HttpPut("{id:guid}")]
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