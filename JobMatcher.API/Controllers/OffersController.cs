using System.Text.Json;
using JobMatcher.API.Infrastructure;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Ai)]
public class OffersController : ControllerBase
{
    private readonly IJobScoringOrchestrator _orchestrator;

    public OffersController(IJobScoringOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("score")]
    public async Task<IActionResult> Score([FromBody] ScoreRequest request, CancellationToken ct)
    {
        var jobs = await _orchestrator
            .StreamScoredJobsAsync(User.GetUserId(), request, ct)
            .OrderByDescending(j => j.Score)
            .ToListAsync(ct);

        return Ok(new { total = jobs.Count, jobs });
    }

    // ct is HttpContext.RequestAborted: closing the tab stops fetching and AI calls
    [HttpPost("score/stream")]
    public async Task ScoreStream([FromBody] ScoreRequest request, CancellationToken ct)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        // Returning IAsyncEnumerable would be buffered, so each line is written and flushed by hand
        await foreach (var job in _orchestrator.StreamScoredJobsAsync(User.GetUserId(), request, ct))
        {
            await Response.WriteAsync(JsonSerializer.Serialize(job, JsonSerializerOptions.Web) + "\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
