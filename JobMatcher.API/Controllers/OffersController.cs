using JobMatcher.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly JobFetcherService _fetcher;

    public OffersController(JobFetcherService fetcher)
    {
        _fetcher = fetcher;
    }

    [HttpGet("fetch")]
    public async Task<IActionResult> FetchOffers()
    {
        var offers = await _fetcher.FetchJuniorOffersAsync();
        return Ok(new
        {
            total = offers.Count,
            offers
        });
    }
}