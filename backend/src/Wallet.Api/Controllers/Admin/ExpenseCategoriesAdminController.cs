using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.ExpenseCategories.Models;

namespace WalletSystem.Api.Controllers.Admin;

/// <summary>Admin-only CRUD for expense categories. Every endpoint requires Role=ADMIN.</summary>
[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("admin/expense-categories")]
public class ExpenseCategoriesAdminController : ControllerBase
{
    private readonly IExpenseCategoryService _service;

    public ExpenseCategoriesAdminController(IExpenseCategoryService service)
    {
        _service = service;
    }

    /// <summary>Lists every category, including INACTIVE ones.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategoryResponse>>>> GetAll(CancellationToken ct)
    {
        try { return Ok((await _service.GetAllAsync(ct)).Data!); }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>Creates a new category. New categories always start as ACTIVE.</summary>
    [HttpPost]
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

    /// <summary>Updates a category's name and description only. Status is changed via the PATCH endpoint.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryResponse>>> Update(
        Guid id,
        [FromBody] ExpenseCategoryRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, ct);
            return StatusCode(result.Status, result);
        }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }

    /// <summary>Activates or deactivates a category. There is no DELETE endpoint - deactivation is how "removal" is expressed.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExpenseCategoryResponse>>> SetStatus(
        Guid id,
        [FromBody] SetExpenseCategoryStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _service.SetStatusAsync(id, request.Status, ct);
            return StatusCode(result.Status, result);
        }
        catch (DomainException ex) { return ex.ToActionResult(this); }
    }
}
