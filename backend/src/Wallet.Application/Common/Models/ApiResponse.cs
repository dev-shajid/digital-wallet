namespace WalletSystem.Application.Common.Models;

/// <summary>
/// The one JSON shape every API response uses - success or failure. Keeps every
/// endpoint predictable for anyone calling it (frontend, Swagger's "Try it out",
/// another service), the same idea as always wrapping an Express response in
/// <c>res.json({ success, message, data })</c> instead of letting each route pick
/// its own shape.
/// </summary>
/// <typeparam name="T">The type of the payload in <see cref="Data"/> on success.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; init; }

    /// <summary>Same value as the HTTP status code, repeated in the body for convenience.</summary>
    public int Status { get; init; }

    /// <summary>Human-readable summary, safe to show to an end user.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The payload on success. Null on failure.</summary>
    public T? Data { get; init; }

    /// <summary>Validation/domain issues on failure. Null when there aren't any to list.</summary>
    public List<ApiError>? Errors { get; init; }
}

/// <summary>One item in <see cref="ApiResponse{T}.Errors"/>, e.g. { field: "email", message: "Email is required." }.</summary>
public class ApiError
{
    /// <summary>The input field this error is about, if any (null for a general/domain error).</summary>
    public string? Field { get; init; }

    public string Message { get; init; } = string.Empty;
}
