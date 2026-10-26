namespace JobMatcher.API.Services.Interfaces;

/// <summary>
/// Sends a single prompt to an LLM provider and returns the raw text answer.
/// Throws <see cref="Ai.AiServiceException"/> when the provider fails.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, double temperature, CancellationToken ct);
}
