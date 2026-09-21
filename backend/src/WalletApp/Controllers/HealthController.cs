using Microsoft.AspNetCore.Mvc;
using WalletApp.Extensions;
using WalletApp.Models.DTOs;

namespace WalletApp.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return this.ApiOk<object?>(null, "Service is healthy.");
    }
}
