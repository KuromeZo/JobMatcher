using System.Text.Json.Serialization;

namespace JobMatcher.API.Models.External.Ai;

// JSON shape the scoring prompt asks the model to return
public class AiScoreResult
{
    public int Score { get; set; }
    public List<string> Matches { get; set; } = [];

    [JsonPropertyName("to_learn")]
    public List<string> ToLearn { get; set; } = [];

    public string Verdict { get; set; } = "";
}
