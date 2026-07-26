using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.External.Claude;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.Claude;

public class ClaudeAiScoringService : IAiScoringService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeAiScoringService> _logger;

    public ClaudeAiScoringService(HttpClient httpClient, ILogger<ClaudeAiScoringService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = config["ClaudeApi:ApiKey"]
                     ?? throw new InvalidOperationException("ClaudeApi:ApiKey is not configured");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ScoredJob?> ScoreJobAsync(JobOffer offer, CandidateProfile profile)
    {
        var jobText = StripHtml(offer.Body ?? "");
        var candidateSkills = string.Join(", ", profile.Skills);
        var requiredSkills = string.Join(", ", offer.RequiredSkills.Select(s => s.Name));

        var jsonTemplate = "{\"score\": <1-10>, \"matches\": [], \"to_learn\": [], \"verdict\": \"\"}";

        var prompt = $"""
                      You are a job match evaluator.

                      CANDIDATE SKILLS: {candidateSkills}

                      JOB: {offer.Title} at {offer.CompanyName}
                      REQUIRED SKILLS: {requiredSkills}
                      JOB DESCRIPTION: {jobText}

                      STEP 1: List every technology/skill in REQUIRED SKILLS and JOB DESCRIPTION.
                      STEP 2: For each item from STEP 1, check if it exists in CANDIDATE SKILLS.
                      STEP 3: Items NOT found in CANDIDATE SKILLS go into to_learn.
                      STEP 4: Items FOUND in CANDIDATE SKILLS go into matches.

                      Return ONLY valid JSON, no markdown, no explanation.
                      Format: {jsonTemplate}
                      - score: 1-10 based on how many required skills candidate has
                      - matches: skills from STEP 4
                      - to_learn: skills from STEP 3 (specific technology names like "React", "Azure", "AWS", "WordPress", "Kafka" — NOT generic phrases)
                      - verdict: one sentence
                      """;

        var requestBody = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 1000,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var text = responseObj
                .GetProperty("content")[0]
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

            var result = JsonSerializer.Deserialize<ClaudeScoreResult>(cleanText,
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
}