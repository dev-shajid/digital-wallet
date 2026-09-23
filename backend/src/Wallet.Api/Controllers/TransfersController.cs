using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Transfers.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// Endpoints for executing peer-to-peer (P2P) transfers and retrieving transfer history.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(Role.USER))]
[Route("transfers")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;

    public TransfersController(ITransferService transferService)
    {
        _transferService = transferService;
    }

    /// <summary>
    /// Executes a P2P money transfer from the authenticated user to another account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransferResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TransferResponse>>> Transfer(
        [FromBody] TransferRequest request,
        CancellationToken ct)
    {
        var result = await _transferService.TransferAsync(User.GetUserId(), request, ct);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Returns the recent P2P transfer history (both sent and received) for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TransferHistoryItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TransferHistoryItem>>>> GetHistory(CancellationToken ct)
    {
        var result = await _transferService.GetHistoryAsync(User.GetUserId(), ct);
        return StatusCode(result.Status, result);
    }
}
