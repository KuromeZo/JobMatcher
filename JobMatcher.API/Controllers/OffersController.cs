using System.Security.Claims;
using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly IJobScoringOrchestrator _orchestrator;
    private readonly IJobFetcherService _fetcher;

    public OffersController(IJobScoringOrchestrator orchestrator, IJobFetcherService fetcher)
    {
        _orchestrator = orchestrator;
        _fetcher = fetcher;
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new InvalidOperationException("User ID not found in token"));

    [HttpGet("fetch")]
    public async Task<IActionResult> FetchOffers()
    {
        var offers = await _fetcher.FetchOffersMetadataAsync([], new JobSearchFilters());
        return Ok(new { total = offers.Count, offers });
    }

    [HttpGet("score")]
    public async Task<IActionResult> ScoreOffers()
    {
        var jobs = await _orchestrator.GetScoredJobsAsync(GetUserId());
        return Ok(new { total = jobs.Count, jobs });
    }

    [HttpPost("score")]
    public async Task<IActionResult> ScoreOffersWithRequest([FromBody] ScoreRequest request)
    {
        var jobs = await _orchestrator.GetScoredJobsAsync(GetUserId(), request);
        return Ok(new { total = jobs.Count, jobs });
    }

    [HttpPost("score/stream")]
    public async Task ScoreOffersStream([FromBody] ScoreRequest request)
    {
        Response.ContentType = "application/x-ndjson";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var job in _orchestrator.GetScoredJobsStreamAsync(GetUserId(), request))
        {
            var json = JsonSerializer.Serialize(job, JsonOptions);
            await Response.WriteAsync(json + "\n");
            await Response.Body.FlushAsync();
        }
    }
}