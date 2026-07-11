namespace JobMatcher.API.Models.External.Claude;

public class ClaudeScoreResult
{
    public int Score { get; set; }
    public List<string> Matches { get; set; } = [];
    public List<string> ToLearn { get; set; } = [];
    public string Verdict { get; set; } = "";
}