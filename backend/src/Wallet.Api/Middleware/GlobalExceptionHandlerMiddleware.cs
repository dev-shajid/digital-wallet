using System.Net;
using Wallet.Application.Common.Exceptions;
using Wallet.Application.Common.Models;

namespace Wallet.Api.Middleware;

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                validation.Message,
                validation.Errors),
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                notFound.Message,
                null),
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                null),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled application exception");
        }

        if (environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            message = exception.Message;
        }

        var body = ApiResponse<object?>.Fail(
            message,
            (int)statusCode,
            errors);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(body);
    }
}
