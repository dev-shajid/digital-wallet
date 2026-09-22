using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WalletSystem.Api.Common;
using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Returns the profile of the currently authenticated user, identified from the access token.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> GetCurrentUser(CancellationToken ct)
    {
        var result = await _authService.GetCurrentUserAsync(User.GetUserId(), ct);
        return StatusCode(result.Status, result);
    }
}
