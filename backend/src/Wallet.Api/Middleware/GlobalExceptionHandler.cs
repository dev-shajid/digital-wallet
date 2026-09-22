using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WalletSystem.Api.Middleware;

/// <summary>
/// Catches any exception that escapes a request and turns it into an RFC 7807
/// "ProblemDetails" JSON response instead of an ASP.NET Core default error page - the
/// .NET equivalent of an Express `app.use((err, req, res, next) => { ... })` catch-all
/// error handler. The real exception message/stack trace is only logged on the server;
/// the client always gets a generic, safe message (never a stack trace).
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "Something went wrong on our side. Please try again later.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
