namespace JobMatcher.API.Models.Domain;

public class ScoredJob
{
    public JobOffer Offer { get; set; } = new();
    public int Score { get; set; }
    public List<string> Matches { get; set; } = [];
    public List<string> ToLearn { get; set; } = [];
    public string Verdict { get; set; } = "";
}