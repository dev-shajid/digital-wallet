using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallet.Models;
using WalletSystem.Domain.Enums;

namespace WalletSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.USER))]
[Route("wallets")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<WalletResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<WalletResponse>>>> GetMyWallets(
        CancellationToken ct)
    {
        var result = await _walletService.GetMyWalletsAsync(
            User.GetUserId(),
            ct);

        return StatusCode(result.Status, result);
    }
}