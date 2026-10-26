using System.Security.Claims;
using System.Threading.RateLimiting;

namespace JobMatcher.API.Infrastructure;

public static class RateLimitPolicies
{
    /// <summary>Login and registration, per IP: slows down password guessing and mass sign-ups.</summary>
    public const string Auth = "auth";

    /// <summary>Endpoints that call the paid AI API, per user.</summary>
    public const string Ai = "ai";

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));

            options.AddPolicy(Ai, context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromHours(1)
                }));
        });

        return services;
    }
}
