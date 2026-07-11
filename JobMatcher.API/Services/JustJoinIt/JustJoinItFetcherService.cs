using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.External.JustJoinIt;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.JustJoinIt;

public class JustJoinItFetcherService : IJobFetcherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JustJoinItFetcherService> _logger;

    private static readonly string[] Categories = ["net", "data", "java", "javascript"];
    private const string BaseUrl = "https://justjoin.it/api/candidate-api/offers";

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

    public async Task<List<JobOffer>> FetchOffersMetadataAsync()
    {
        var allOffers = new List<JobOffer>();

        foreach (var category in Categories)
        {
            _logger.LogInformation("Fetching category: {Category}", category);
            var offers = await FetchCategoryAsync(category);
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

    private async Task<List<JobOffer>> FetchCategoryAsync(string category)
    {
        var offers = new List<JobOffer>();
        int? cursor = null;
        int pageCount = 0;
        const int maxPages = 10;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        do
        {
            var url = BuildUrl(category, cursor);

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JustJoinItResponse>(json, options);

                if (result?.Data == null) break;

                offers.AddRange(result.Data);
                pageCount++;

                cursor = result.Meta?.Next?.Cursor.HasValue == true && result.Data.Count > 0
                    ? (cursor ?? 0) + result.Data.Count
                    : null;

                _logger.LogInformation("Category {Category} page {Page}/{Max}, got {Count} offers",
                    category, pageCount, maxPages, result.Data.Count);

                await Task.Delay(300);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch category {Category} cursor {Cursor}",
                    category, cursor);
                break;
            }

        } while (cursor.HasValue && pageCount < maxPages);

        return offers;
    }

    private static string BuildUrl(string category, int? cursor)
    {
        var url = $"{BaseUrl}?categories={category}&experienceLevels=junior&sortBy=publishedAt&orderBy=descending";
        if (cursor.HasValue)
            url += $"&cursor={cursor.Value}";
        return url;
    }
}