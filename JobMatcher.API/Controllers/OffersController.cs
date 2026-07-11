using JobMatcher.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var offers = await _fetcher.FetchJuniorOffersAsync();
        return Ok(new { total = offers.Count, offers });
    }

    [HttpGet("score")]
    public async Task<IActionResult> ScoreOffers()
    {
        var jobs = await _orchestrator.GetScoredJobsAsync();
        return Ok(new { total = jobs.Count, jobs });
    }
}