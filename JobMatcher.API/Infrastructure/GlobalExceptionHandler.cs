using JobMatcher.API.Services.Ai;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Infrastructure;

/// <summary>Turns unhandled exceptions into ProblemDetails responses without leaking internals.</summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            AiServiceException => (StatusCodes.Status502BadGateway, "The AI service is unavailable, please try again later"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
