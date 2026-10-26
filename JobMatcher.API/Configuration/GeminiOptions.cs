using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Configuration;

public class GeminiOptions
{
    public const string SectionName = "GeminiApi";

    [Required]
    public string ApiKey { get; set; } = "";

    [Required, Url]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/";

    [Required]
    public string Model { get; set; } = "gemini-2.5-flash";

    // Gemini 2.5 models spend part of this budget on thinking, so it is higher than Claude's
    [Range(1, 65536)]
    public int MaxOutputTokens { get; set; } = 4096;
}
