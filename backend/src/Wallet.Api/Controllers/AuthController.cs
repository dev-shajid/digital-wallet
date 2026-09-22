using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Common.Models;

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
    // Requirements
    // The interface should be defined in:
    // Wallet.Application/Abstractions/IAuthService.cs
    private readonly IAuthService _authService;

    // ASP.NET Core injects the teammate's AuthService implementation here.
    // Teammate's implementation should be registered in:
    // Wallet.Infrastructure/DependencyInjection.cs
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/v1/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(
        [FromBody] RegisterRequest request)
    {
        // Requirements
        // RegisterAsync() should be implemented in the authentication
        // service, probably in:
        // Wallet.Application/Auth/AuthService.cs
        //
        // The service should handle:
        // - validating registration data
        // - checking email uniqueness
        // - generating account number
        // - hashing password
        // - creating User
        // - creating default BDT Wallet
        // - saving everything atomically
        var result = await _authService.RegisterAsync(request);

        // Controller's responsibility:
        // Return the status code and response received from the service.
        return StatusCode(result.Status, result);
    }

    // POST /api/v1/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
    {
        // Requirements
        // LoginAsync() should be implemented in the authentication
        // service, probably in:
        // Wallet.Application/Auth/AuthService.cs
        //
        // The service should handle:
        // - finding the user
        // - verifying the password
        // - generating JWT
        // - creating LoginResponse
        var result = await _authService.LoginAsync(request);

        // Controller's responsibility:
        // Return the status code and response received from the service.
        return StatusCode(result.Status, result);
    }
}