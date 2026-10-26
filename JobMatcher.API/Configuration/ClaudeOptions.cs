using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Configuration;

public class ClaudeOptions
{
    public const string SectionName = "ClaudeApi";

    [Required]
    public string ApiKey { get; set; } = "";

    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";

    [Required]
    public string ApiVersion { get; set; } = "2023-06-01";

    [Required]
    public string Model { get; set; } = "claude-haiku-4-5-20251001";

    [Range(1, 8192)]
    public int MaxTokens { get; set; } = 1000;
}
