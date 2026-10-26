using System.Text.Json;

namespace JobMatcher.API.Services.Ai;

public static class LlmJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Extracts the JSON object from an LLM answer. Models sometimes wrap it in ```json fences
    /// or add a sentence around it, so everything outside the outermost braces is ignored.
    /// Returns null if there is no valid JSON object.
    /// </summary>
    public static T? TryParse<T>(string text) where T : class
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            return JsonSerializer.Deserialize<T>(text.AsSpan(start, end - start + 1), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
