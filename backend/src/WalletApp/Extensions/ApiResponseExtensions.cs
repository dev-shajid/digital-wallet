using Microsoft.AspNetCore.Mvc;
using WalletApp.Models.DTOs;

namespace WalletApp.Extensions;

public static class ApiResponseExtensions
{
    public static IActionResult ApiOk<T>(this ControllerBase controller, T data, string message = "Request completed successfully.")
    {
        return controller.StatusCode(StatusCodes.Status200OK, ApiResponse<T>.Ok(data, message));
    }

    public static IActionResult ApiCreated<T>(this ControllerBase controller, T data, string message = "Resource created successfully.")
    {
        return controller.StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message, StatusCodes.Status201Created));
    }

    public static IActionResult ApiFail(
        this ControllerBase controller,
        string message,
        int statusCode,
        IReadOnlyList<ApiError>? errors = null)
    {
        return controller.StatusCode(statusCode, ApiResponse<object?>.Fail(message, statusCode, errors));
    }
}
