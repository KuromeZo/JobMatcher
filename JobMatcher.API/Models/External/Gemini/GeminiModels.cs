namespace JobMatcher.API.Models.External.Gemini;

public class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; } = [];
    public GeminiGenerationConfig GenerationConfig { get; set; } = new();
}

public class GeminiContent
{
    public List<GeminiPart> Parts { get; set; } = [];
}

public class GeminiPart
{
    public string? Text { get; set; }
}

public class GeminiGenerationConfig
{
    public double Temperature { get; set; }
    public int MaxOutputTokens { get; set; }
    public string? ResponseMimeType { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate> Candidates { get; set; } = [];
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
    public string? FinishReason { get; set; }
}
