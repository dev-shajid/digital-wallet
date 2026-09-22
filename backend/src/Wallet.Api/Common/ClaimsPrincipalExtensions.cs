using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WalletSystem.Api.Common;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The authenticated user's id, from the JWT "sub" claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The access token has no valid subject claim.");
        }

        return userId;
    }
}
