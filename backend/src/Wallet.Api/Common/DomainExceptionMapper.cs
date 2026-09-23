using Microsoft.AspNetCore.Mvc;
using WalletSystem.Application.Common.Exceptions;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Api.Common;

/// <summary>Maps a domain-level failure thrown by an Application/Infrastructure service
/// to the same <see cref="ApiResponse{T}"/> envelope every other endpoint uses.</summary>
public static class DomainExceptionMapper
{
    public static ActionResult ToActionResult(this DomainException ex, ControllerBase controller)
    {
        var errors = string.IsNullOrEmpty(ex.Field)
            ? null
            : new List<ApiError> { new() { Field = ex.Field, Message = ex.Message } };

        return controller.StatusCode(ex.StatusCode, new ApiResponse<object?>
        {
            Success = false,
            Status = ex.StatusCode,
            Message = ex.Message,
            Data = null,
            Errors = errors
        });
    }
}
