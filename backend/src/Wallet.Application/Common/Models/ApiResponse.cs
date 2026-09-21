namespace Wallet.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public required int Status { get; init; }

    public required bool Success { get; init; }

    public required string Message { get; init; }

    public T? Data { get; init; }

    public IReadOnlyList<ApiError>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully.", int status = 200)
    {
        return new ApiResponse<T>
        {
            Status = status,
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
        };
    }

    public static ApiResponse<T> Fail(
        string message,
        int status,
        IReadOnlyList<ApiError>? errors = null,
        T? data = default)
    {
        return new ApiResponse<T>
        {
            Status = status,
            Success = false,
            Message = message,
            Data = data,
            Errors = errors,
        };
    }
}

public static class ApiResponse
{
    public static ApiResponse<object?> Ok(object? data, string message = "Request completed successfully.", int status = 200)
        => ApiResponse<object?>.Ok(data, message, status);

    public static ApiResponse<object?> Fail(
        string message,
        int status,
        IReadOnlyList<ApiError>? errors = null,
        object? data = null)
        => ApiResponse<object?>.Fail(message, status, errors, data);
}
