using System.Security.Claims;

namespace JobMatcher.API.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id of the authenticated user. Only call it on endpoints with [Authorize].</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("User ID not found in token"));
}
