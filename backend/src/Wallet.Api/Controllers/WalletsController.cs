using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallets.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Api.Controllers;

/// <summary>
/// Endpoints for querying user wallet information.
/// </summary>
[ApiController]
[Authorize(Roles = nameof(Role.USER))]
[Route("wallets")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>
    /// Returns the default BDT wallet and balance for the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<WalletBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<WalletBalanceResponse>>> GetMyWallet(CancellationToken ct)
    {
        var result = await _walletService.GetMyBdtWalletAsync(User.GetUserId(), ct);
        return StatusCode(result.Status, result);
    }
}
