using JobMatcher.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly JobFetcherService _fetcher;
    private readonly ScoringService _scorer;

    public OffersController(JobFetcherService fetcher, ScoringService scorer)
    {
        _fetcher = fetcher;
        _scorer = scorer;
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
        var offers = await _fetcher.FetchJuniorOffersAsync();

        var scoredJobs = new List<object>();

        foreach (var offer in offers)
        {
            var scored = await _scorer.ScoreJobAsync(offer);
            if (scored != null && scored.Score >= 6)
                scoredJobs.Add(scored);
        }

        var sorted = scoredJobs
            .OrderByDescending(s => ((dynamic)s).Score)
            .ToList();

        return Ok(new { total = sorted.Count, jobs = sorted });
    }
}