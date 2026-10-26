namespace JobMatcher.API.Models.External.Claude;

// Serialized with snake_case naming, see ClaudeLlmClient

public class ClaudeMessagesRequest
{
    public string Model { get; set; } = "";
    public int MaxTokens { get; set; }
    public double Temperature { get; set; }
    public List<ClaudeMessage> Messages { get; set; } = [];
}

public class ClaudeMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}

public class ClaudeMessagesResponse
{
    public List<ClaudeContentBlock> Content { get; set; } = [];
    public string? StopReason { get; set; }
}

public class ClaudeContentBlock
{
    public string Type { get; set; } = "";
    public string? Text { get; set; }
}
