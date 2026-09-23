namespace WalletSystem.Application.Common.Exceptions;

/// <summary>
/// Thrown by Application/Infrastructure services to signal a domain-level failure
/// that maps cleanly to a specific HTTP status. Controllers convert these via the
/// shared mapper into the standard <c>ApiResponse&lt;object?&gt;</c> envelope.
///
/// HTTP status constants are inlined here to keep this project free of any
/// ASP.NET Core reference (Application layer doesn't depend on the web stack).
/// </summary>
public class DomainException : Exception
{
    public int StatusCode { get; }
    public string? Field { get; }

    public DomainException(int statusCode, string message, string? field = null)
        : base(message)
    {
        StatusCode = statusCode;
        Field = field;
    }

    public const int HttpBadRequest = 400;
    public const int HttpNotFound = 404;
    public const int HttpConflict = 409;
    public const int HttpUnprocessableEntity = 422;

    public static DomainException NotFound(string message, string? field = null)
        => new(HttpNotFound, message, field);

    public static DomainException Conflict(string message, string? field = null)
        => new(HttpConflict, message, field);

    public static DomainException BadRequest(string message, string? field = null)
        => new(HttpBadRequest, message, field);
}
