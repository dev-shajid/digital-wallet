using Microsoft.AspNetCore.Mvc;

using WalletSystem.Application.Abstractions;
using WalletSystem.Application.Auth.Models;
using WalletSystem.Application.Common.Models;

// Requirements
// IAuthService should be created in:
// Wallet.Application/Abstractions/IAuthService.cs

// Requirements
// RegisterRequest, RegisterResponse, LoginRequest, LoginResponse
// should be created in something like:
// Wallet.Application/Auth/Models/
// or another appropriate Application folder.

namespace WalletSystem.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user and automatically creates their default BDT wallet.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Authenticates a user with email and password and returns a JWT access token plus a refresh token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Exchanges a still-valid refresh token for a new access + refresh token pair. The refresh
    /// token used is rotated (revoked) as part of this call.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return StatusCode(result.Status, result);
    }

    /// <summary>
    /// Revokes a refresh token so it can no longer be used to obtain new access tokens.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<object?>>> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request);
        return StatusCode(result.Status, result);
    }
}