using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Api.Common;

public static class ApiResponseExtensions
{
    public static ObjectResult ApiOk<T>(this ControllerBase controller, T data, string message = "Request completed successfully.")
        => controller.StatusCode(StatusCodes.Status200OK, new ApiResponse<T>
        {
            Success = true,
            Status = StatusCodes.Status200OK,
            Message = message,
            Data = data
        });

    public static ObjectResult ApiCreated<T>(this ControllerBase controller, T data, string message = "Resource created successfully.")
        => controller.StatusCode(StatusCodes.Status201Created, new ApiResponse<T>
        {
            Success = true,
            Status = StatusCodes.Status201Created,
            Message = message,
            Data = data
        });

    public static ObjectResult ApiFail(
        this ControllerBase controller,
        string message,
        int status = StatusCodes.Status400BadRequest,
        List<ApiError>? errors = null)
        => controller.StatusCode(status, new ApiResponse<object?>
        {
            Success = false,
            Status = status,
            Message = message,
            Data = null,
            Errors = errors
        });
}
