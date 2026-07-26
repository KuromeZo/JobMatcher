using System.Text.Json.Serialization;

namespace JobMatcher.API.Models.External.Claude;

public class ClaudeScoreResult
{
    public int Score { get; set; }
    public List<string> Matches { get; set; } = [];

    [JsonPropertyName("to_learn")]
    public List<string> ToLearn { get; set; } = [];

    public string Verdict { get; set; } = "";
}