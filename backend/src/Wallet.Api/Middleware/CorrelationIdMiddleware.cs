using Serilog.Context;

namespace WalletSystem.Api.Middleware;

/// <summary>
/// Gives every request a "Correlation Id" - like a request id you'd generate in Express
/// with a `req.id = crypto.randomUUID()` middleware. It is read from the incoming
/// "X-Correlation-Id" header if the caller already sent one, otherwise a new one is
/// generated. It's echoed back on the response and attached to every log line written
/// while handling the request, so all logs for one request can be found by searching
/// for one id.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
