using Microsoft.AspNetCore.Mvc;
using WalletSystem.Api.Common;

namespace WalletSystem.Api.Controllers;

// "api/v1" is glued onto this automatically by ApiPrefixConvention (registered in
// Program.cs), so the actual route ends up being "api/v1/diagnostics".
[ApiController]
[Route("diagnostics")]
public class DiagnosticsController : ControllerBase
{=
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
        => this.ApiOk(new { status = "Healthy" }, "API is up and running.");

    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ThrowTestError()
    {
        throw new InvalidOperationException("This is a deliberate test exception from DiagnosticsController - it confirms the global exception handler is working.");
    }
}
