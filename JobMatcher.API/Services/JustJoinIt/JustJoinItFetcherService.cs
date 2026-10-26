using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.External.JustJoinIt;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.JustJoinIt;

// BaseAddress and browser-like headers are configured in AddJobBoard
public class JustJoinItFetcherService : IJobFetcherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan PageDelay = TimeSpan.FromMilliseconds(300);

    private const int ConsecutiveKnownThreshold = 5;
    private const int MaxPages = 2;
    private const int PageSize = 10;

    private readonly HttpClient _httpClient;
    private readonly ILogger<JustJoinItFetcherService> _logger;

    public JustJoinItFetcherService(HttpClient httpClient, ILogger<JustJoinItFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<JobOffer>> FetchOffersMetadataAsync(
        IReadOnlySet<string> knownGuids, JobSearchFilters filters, CancellationToken ct)
    {
        var allOffers = new List<JobOffer>();

        foreach (var category in filters.Categories)
        {
            _logger.LogInformation("Fetching category: {Category}", category);
            var offers = await FetchCategoryAsync(category, knownGuids, filters.ExperienceLevels, ct);
            allOffers.AddRange(offers);
        }

        var uniqueOffers = allOffers
            .DistinctBy(o => o.Guid)
            .ToList();

        _logger.LogInformation("Fetched {Total} unique offers across all categories", uniqueOffers.Count);

        return uniqueOffers;
    }

    public async Task<string?> FetchOfferBodyAsync(string slug, CancellationToken ct)
    {
        try
        {
            var offer = await _httpClient.GetFromJsonAsync<JobOffer>(
                $"offers/{Uri.EscapeDataString(slug)}", JsonOptions, ct);
            return offer?.Body;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to fetch body for {Slug}", slug);
            return null;
        }
    }

    private async Task<List<JobOffer>> FetchCategoryAsync(
        string category, IReadOnlySet<string> knownGuids, List<string> levels, CancellationToken ct)
    {
        var offers = new List<JobOffer>();
        var offset = 0;
        var consecutiveKnown = 0;

        for (var page = 1; page <= MaxPages; page++)
        {
            JustJoinItResponse? result;
            try
            {
                result = await _httpClient.GetFromJsonAsync<JustJoinItResponse>(
                    BuildUrl(category, levels, offset), JsonOptions, ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to fetch category {Category} offset {Offset}", category, offset);
                break;
            }

            if (result?.Data == null || result.Data.Count == 0) break;

            foreach (var offer in result.Data)
            {
                // The listing does not always include category and level, so they are taken from the request
                if (offer.Category == null || string.IsNullOrEmpty(offer.Category.Key))
                    offer.Category = new JobCategory { Key = category };

                if (string.IsNullOrEmpty(offer.ExperienceLevel))
                    offer.ExperienceLevel = levels.FirstOrDefault() ?? "";

                offers.Add(offer);
                consecutiveKnown = knownGuids.Contains(offer.Guid) ? consecutiveKnown + 1 : 0;

                // Results are sorted by newest, so a streak of known offers means the rest is known too
                if (consecutiveKnown >= ConsecutiveKnownThreshold)
                {
                    _logger.LogInformation(
                        "Stopping category {Category} - found {Streak} consecutive known offers",
                        category, consecutiveKnown);
                    return offers;
                }
            }

            offset += result.Data.Count;

            _logger.LogInformation("Category {Category} page {Page}/{Max}, got {Count} offers, offset now: {Offset}",
                category, page, MaxPages, result.Data.Count, offset);

            if (page < MaxPages)
                await Task.Delay(PageDelay, ct);
        }

        return offers;
    }

    private static string BuildUrl(string category, List<string> levels, int offset)
    {
        var url = $"offers?categories={Uri.EscapeDataString(category)}" +
                  $"&sortBy=newest&orderBy=descending&itemsCount={PageSize}&from={offset}";

        foreach (var level in levels)
            url += $"&experienceLevels={Uri.EscapeDataString(level)}";

        return url;
    }
}
