using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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

    [HttpGet("fetch")]
    public async Task<IActionResult> FetchOffers()
    {
        var offers = await _fetcher.FetchOffersMetadataAsync([]);
        return Ok(new { total = offers.Count, offers });
    }

    [HttpGet("score")]
    public async Task<IActionResult> ScoreOffers()
    {
        var jobs = await _orchestrator.GetScoredJobsAsync();
        return Ok(new { total = jobs.Count, jobs });
    }

    [HttpPost("score")]
    public async Task<IActionResult> ScoreOffersWithProfile([FromBody] CandidateProfile profile)
    {
        var jobs = await _orchestrator.GetScoredJobsAsync(profile);
        return Ok(new { total = jobs.Count, jobs });
    }
}