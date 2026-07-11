namespace JobMatcher.API.Models.Domain;

public class ScoreRequest
{
    public CandidateProfile Profile { get; set; } = new();
    public JobSearchFilters Filters { get; set; } = new();
    
    public int MinScore { get; set; } = 6;
    
    public bool ForceRescore { get; set; } = false;
}