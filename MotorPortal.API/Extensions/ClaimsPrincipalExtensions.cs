using System.Security.Claims;

namespace MotorPortal.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(value) || !long.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("The current user does not have a valid user_id claim.");
        }

        return userId;
    }
}
