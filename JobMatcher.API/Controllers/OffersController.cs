using JobMatcher.API.Repositories;
using JobMatcher.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly JobFetcherService _fetcher;
    private readonly ScoringService _scorer;
    private readonly JobRepository _repo;

    public OffersController(JobFetcherService fetcher, ScoringService scorer, JobRepository repo)
    {
        _fetcher = fetcher;
        _scorer = scorer;
        _repo = repo;
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
            // Сначала проверяем кеш
            var cached = await _repo.GetCachedAsync(offer.Guid);
            if (cached != null)
            {
                cached.Offer = offer;
                if (cached.Score >= 6)
                    scoredJobs.Add(cached);
                continue;
            }

            // Нет в кеше — спрашиваем Claude
            var scored = await _scorer.ScoreJobAsync(offer);
            if (scored != null)
            {
                await _repo.SaveAsync(offer.Guid, scored);
                if (scored.Score >= 6)
                    scoredJobs.Add(scored);
            }
        }

        var sorted = scoredJobs
            .OrderByDescending(s => ((dynamic)s).Score)
            .ToList();

        return Ok(new { total = sorted.Count, jobs = sorted });
    }
}