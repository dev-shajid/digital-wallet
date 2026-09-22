using Microsoft.AspNetCore.Diagnostics;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Api.Middleware;

/// <summary>
/// Catches any exception that escapes a request and turns it into the same
/// <see cref="ApiResponse{T}"/> envelope every other endpoint uses, instead of an
/// ASP.NET Core default error page - the .NET equivalent of an Express
/// <c>app.use((err, req, res, next) => { ... })</c> catch-all error handler. The real
/// exception message/stack trace is only ever logged on the server; the client always
/// gets a generic, safe message.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        const int status = StatusCodes.Status500InternalServerError;

        var response = new ApiResponse<object?>
        {
            Success = false,
            Status = status,
            Message = "Something went wrong on our side. Please try again later.",
            Data = null
        };

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
