using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.Gemini;

public class GeminiAiScoringService : IAiScoringService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiScoringService> _logger;
    private readonly string _apiKey;

    private const string Model = "gemini-2.5-flash";

    public GeminiAiScoringService(HttpClient httpClient, ILogger<GeminiAiScoringService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = config["GeminiApi:ApiKey"]
                  ?? throw new InvalidOperationException("GeminiApi:ApiKey is not configured");
    }

    public async Task<ScoredJob?> ScoreJobAsync(JobOffer offer, CandidateProfile profile)
    {
        var profileText = $"Level: {profile.Level}\nDescription: {profile.Description}\nSkills: {string.Join(", ", profile.Skills)}";
        var jobText = StripHtml(offer.Body ?? "");

        var jsonTemplate = "{\"score\": <1-10>, \"matches\": [], \"to_learn\": [], \"verdict\": \"\"}";

        var prompt = $"""
                      You are a job match evaluator for a developer.

                      Candidate profile:
                      {profileText}

                      Job offer: {offer.Title} at {offer.CompanyName}
                      Required skills: {string.Join(", ", offer.RequiredSkills.Select(s => s.Name))}

                      Job description:
                      {jobText}

                      Evaluate this job for the candidate. Return ONLY valid JSON, no markdown, no explanation.
                      Format: {jsonTemplate}
                      Where score is 1-10, matches is array of matching skills, to_learn is array of skills the candidate is MISSING or needs to improve (be specific and realistic), verdict is one sentence.
                      """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.3,
                maxOutputTokens = 1000
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var text = responseObj
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var cleanText = text.Trim();
            if (cleanText.StartsWith("```"))
            {
                cleanText = cleanText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            }

            var result = JsonSerializer.Deserialize<GeminiScoreResult>(cleanText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null) return null;

            return new ScoredJob
            {
                Offer = offer,
                Score = result.Score,
                Matches = result.Matches,
                ToLearn = result.ToLearn,
                Verdict = result.Verdict
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to score job {Title}", offer.Title);
            return null;
        }
    }

    private static string StripHtml(string html)
    {
        return Regex.Replace(html, "<.*?>", " ").Trim();
    }

    private class GeminiScoreResult
    {
        public int Score { get; set; }
        public List<string> Matches { get; set; } = [];
        public List<string> ToLearn { get; set; } = [];
        public string Verdict { get; set; } = "";
    }
}