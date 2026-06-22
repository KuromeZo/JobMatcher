using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobMatcher.API.Models;

namespace JobMatcher.API.Services;

public class ScoringService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScoringService> _logger;
    private readonly IConfiguration _config;

    public ScoringService(HttpClient httpClient, ILogger<ScoringService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;
        _config = config;

        var apiKey = _config["ClaudeApi:ApiKey"];
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ScoredJob?> ScoreJobAsync(JobOffer offer)
    {
        var profile = BuildProfileString();
        var jobText = StripHtml(offer.Body ?? "");

        var jsonTemplate = "{\"score\": <1-10>, \"matches\": [], \"to_learn\": [], \"verdict\": \"\"}";

        var prompt = $"""
                      You are a job match evaluator for a junior developer.

                      Candidate profile:
                      {profile}

                      Job offer: {offer.Title} at {offer.CompanyName}
                      Required skills: {string.Join(", ", offer.RequiredSkills.Select(s => s.Name))}

                      Job description:
                      {jobText}

                      Evaluate this job for the candidate. Return ONLY valid JSON, no markdown, no explanation.
                      Format: {jsonTemplate}
                      Where score is 1-10, matches is array of matching skills, to_learn is array of gaps, verdict is one sentence.
                      """;

        var requestBody = new
        {
            model = "claude-sonnet-4-6",
            max_tokens = 1000,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error {Status}: {Body}", 
                    response.StatusCode, errorBody);
                return null;
            }
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var text = responseObj
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            var result = JsonSerializer.Deserialize<ScoreResult>(text,
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
        catch (HttpRequestException ex)
        {
            var errorBody = "";
            // уже выброшено после EnsureSuccessStatusCode, нужно поймать до
            _logger.LogError(ex, "Failed to score job {Title}", offer.Title);
            return null;
        }
    }

    private string BuildProfileString()
    {
        var skills = _config.GetSection("CandidateProfile:Skills").Get<List<string>>() ?? [];
        var level = _config["CandidateProfile:Level"] ?? "junior";
        var description = _config["CandidateProfile:Description"] ?? "";

        return $"Level: {level}\nDescription: {description}\nSkills: {string.Join(", ", skills)}";
    }

    private static string StripHtml(string html)
    {
        return Regex.Replace(html, "<.*?>", " ").Trim();
    }
}

public class ScoreResult
{
    public int Score { get; set; }
    public List<string> Matches { get; set; } = [];
    public List<string> ToLearn { get; set; } = [];
    public string Verdict { get; set; } = "";
}