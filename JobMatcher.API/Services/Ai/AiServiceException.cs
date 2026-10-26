namespace JobMatcher.API.Services.Ai;

/// <summary>The AI provider failed or returned something unusable.</summary>
public class AiServiceException : Exception
{
    public AiServiceException(string message) : base(message) { }
}
