using System.Text.Json;
using JobMatcher.API.Configuration;
using JobMatcher.API.Models.External.Claude;
using JobMatcher.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace JobMatcher.API.Services.Ai;

// BaseAddress and auth headers are configured in AddAiServices
public class ClaudeLlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;
    private readonly ILogger<ClaudeLlmClient> _logger;

    public ClaudeLlmClient(HttpClient httpClient, IOptions<ClaudeOptions> options, ILogger<ClaudeLlmClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, double temperature, CancellationToken ct)
    {
        var request = new ClaudeMessagesRequest
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = temperature,
            Messages = [new ClaudeMessage { Role = "user", Content = prompt }]
        };

        // Buffered so the request has a Content-Length instead of chunked encoding
        using var content = JsonContent.Create(request, options: JsonOptions);
        await content.LoadIntoBufferAsync(ct);

        using var response = await _httpClient.PostAsync("v1/messages", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Claude API error {Status}: {Body}", (int)response.StatusCode, errorBody);
            throw new AiServiceException($"Claude API returned {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<ClaudeMessagesResponse>(JsonOptions, ct);
        var text = result?.Content.FirstOrDefault(b => b.Type == "text")?.Text;

        if (string.IsNullOrWhiteSpace(text))
            throw new AiServiceException("Claude API returned an empty response");

        if (result!.StopReason == "max_tokens")
            _logger.LogWarning("Claude response was cut off at {MaxTokens} tokens", _options.MaxTokens);

        return text;
    }
}
