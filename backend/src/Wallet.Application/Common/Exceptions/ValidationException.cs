using Wallet.Application.Common.Models;

namespace Wallet.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(string message, IReadOnlyList<ApiError> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyList<ApiError> Errors { get; }
}
