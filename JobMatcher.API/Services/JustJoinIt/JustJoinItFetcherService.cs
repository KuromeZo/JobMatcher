using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.External.JustJoinIt;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.JustJoinIt;

public class JustJoinItFetcherService : IJobFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JustJoinItFetcherService> _logger;

    private const string BaseUrl = "https://justjoin.it/api/candidate-api/offers";
    private const int ConsecutiveKnownThreshold = 5;

    public JustJoinItFetcherService(HttpClient httpClient, ILogger<JustJoinItFetcherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Linux; Android 15; Pixel 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Mobile Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept",
            "application/json, text/plain, */*");
        _httpClient.DefaultRequestHeaders.Add("Referer",
            "https://justjoin.it/job-offers/all-locations");
    }

    public async Task<List<JobOffer>> FetchOffersMetadataAsync(HashSet<string> existingGuids, JobSearchFilters filters)
    {
        var allOffers = new List<JobOffer>();

        foreach (var category in filters.Categories)
        {
            _logger.LogInformation("Fetching category: {Category}", category);
            var offers = await FetchCategoryAsync(category, existingGuids, filters.ExperienceLevels);
            allOffers.AddRange(offers);
        }

        var uniqueOffers = allOffers
            .DistinctBy(o => o.Guid)
            .ToList();

        _logger.LogInformation("Fetched {Total} unique offers across all categories", uniqueOffers.Count);

        return uniqueOffers;
    }

    public async Task<string?> FetchOfferBodyAsync(string slug)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var url = $"{BaseUrl}/{slug}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var offer = JsonSerializer.Deserialize<JobOffer>(json, options);
            return offer?.Body;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch body for {Slug}", slug);
            return null;
        }
    }

    private async Task<List<JobOffer>> FetchCategoryAsync(string category, HashSet<string> existingGuids, List<string> levels)
    {
        var offers = new List<JobOffer>();
        int offset = 0;
        int pageCount = 0;
        const int maxPages = 2;
        int consecutiveKnown = 0;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        do
        {
            var url = BuildUrl(category, levels, offset);

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JustJoinItResponse>(json, options);

                if (result?.Data == null || result.Data.Count == 0) break;

                bool shouldStop = false;

                foreach (var offer in result.Data)
                {
                    // Заполняем Category и ExperienceLevel из параметров запроса,
                    // т.к. JustJoinIT не всегда возвращает их в листинге
                    if (offer.Category == null || string.IsNullOrEmpty(offer.Category.Key))
                        offer.Category = new JobCategory { Key = category };

                    if (string.IsNullOrEmpty(offer.ExperienceLevel))
                        offer.ExperienceLevel = levels.FirstOrDefault() ?? "";

                    if (existingGuids.Contains(offer.Guid))
                    {
                        consecutiveKnown++;
                        offers.Add(offer);
                        if (consecutiveKnown >= ConsecutiveKnownThreshold)
                        {
                            _logger.LogInformation(
                                "Stopping category {Category} - found {Streak} consecutive known offers",
                                category, consecutiveKnown);
                            shouldStop = true;
                            break;
                        }
                    }
                    else
                    {
                        consecutiveKnown = 0;
                        offers.Add(offer);
                    }
                }

                if (shouldStop) break;

                offset += result.Data.Count;
                pageCount++;

                _logger.LogInformation("Category {Category} page {Page}/{Max}, got {Count} offers, offset now: {Offset}",
                    category, pageCount, maxPages, result.Data.Count, offset);

                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch category {Category} offset {Offset}",
                    category, offset);
                break;
            }

        } while (pageCount < maxPages);

        return offers;
    }

    private static string BuildUrl(string category, List<string> levels, int offset)
    {
        var url = $"{BaseUrl}?categories={category}&sortBy=newest&orderBy=descending&itemsCount=10&from={offset}";

        foreach (var level in levels)
            url += $"&experienceLevels={level}";

        return url;
    }
}