using JobMatcher.API.Configuration;
using JobMatcher.API.Models.External.Gemini;
using JobMatcher.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace JobMatcher.API.Services.Ai;

// BaseAddress and the API key header are configured in AddAiServices
public class GeminiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiLlmClient> _logger;

    public GeminiLlmClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiLlmClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, double temperature, CancellationToken ct)
    {
        var request = new GeminiRequest
        {
            Contents = [new GeminiContent { Parts = [new GeminiPart { Text = prompt }] }],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = temperature,
                MaxOutputTokens = _options.MaxOutputTokens,
                // Every prompt in the app asks for JSON, so JSON mode is always on
                ResponseMimeType = "application/json"
            }
        };

        // Buffered so the request has a Content-Length instead of chunked encoding
        using var content = JsonContent.Create(request);
        await content.LoadIntoBufferAsync(ct);

        using var response = await _httpClient.PostAsync(
            $"v1beta/models/{_options.Model}:generateContent", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Gemini API error {Status}: {Body}", (int)response.StatusCode, errorBody);
            throw new AiServiceException($"Gemini API returned {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(ct);
        var candidate = result?.Candidates.FirstOrDefault();
        var text = candidate?.Content?.Parts.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(text))
            throw new AiServiceException($"Gemini API returned an empty response ({candidate?.FinishReason})");

        if (candidate!.FinishReason == "MAX_TOKENS")
            _logger.LogWarning("Gemini response was cut off at {MaxTokens} tokens", _options.MaxOutputTokens);

        return text;
    }
}
