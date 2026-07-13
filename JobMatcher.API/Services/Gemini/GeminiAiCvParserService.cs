using System.Text;
using System.Text.Json;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services.Gemini;

public class GeminiAiCvParserService : IAiCvParserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiCvParserService> _logger;
    private readonly string _apiKey;

    private const string Model = "gemini-2.5-flash";

    public GeminiAiCvParserService(HttpClient httpClient, ILogger<GeminiAiCvParserService> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = config["GeminiApi:ApiKey"]
                  ?? throw new InvalidOperationException("GeminiApi:ApiKey is not configured");
    }

    public async Task<CandidateProfile> ParseCvAsync(string cvText)
    {
        var jsonFormat = "{\"skills\": [], \"level\": \"\", \"description\": \"\"}";

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
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
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
                return new CandidateProfile();
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