namespace JobMatcher.API.Configuration;

public enum AiProvider
{
    Claude,
    Gemini
}

public class AiOptions
{
    public const string SectionName = "Ai";

    public AiProvider Provider { get; set; } = AiProvider.Claude;
}
