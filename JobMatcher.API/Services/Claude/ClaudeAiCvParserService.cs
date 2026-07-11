using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.Claude;

public class ClaudeAiCvParserService : IAiCvParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeAiCvParserService> _logger;

    public ClaudeAiCvParserService(HttpClient httpClient, ILogger<ClaudeAiCvParserService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        var apiKey = config["ClaudeApi:ApiKey"]
                     ?? throw new InvalidOperationException("ClaudeApi:ApiKey is not configured");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<CandidateProfile> ParseCvAsync(string cvText)
    {
        var jsonFormat = """{"skills": [], "level": "", "description": ""}""";

        var prompt = $"""
                      You are a CV parser. Extract candidate information from the following CV text.

                      CV Text:
                      {cvText}

                      Return ONLY valid JSON, no markdown, no explanation.
                      Format: {jsonFormat}
                      Where:
                      - skills: array of technical skills extracted from CV (programming languages, frameworks, tools)
                      - level: one of "junior", "mid", "senior" based on experience
                      - description: one sentence summary of the candidate
                      """;

        var requestBody = new
        {
            model = "claude-haiku-4-5-20251001",
            max_tokens = 1000,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error {Status}: {Body}", response.StatusCode, errorBody);
                return new CandidateProfile();
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

            var result = JsonSerializer.Deserialize<CandidateProfile>(cleanText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? new CandidateProfile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CV");
            return new CandidateProfile();
        }
    }
}