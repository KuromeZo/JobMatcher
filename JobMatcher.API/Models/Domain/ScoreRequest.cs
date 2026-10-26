using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Domain;

public class ScoreRequest
{
    [Required]
    public CandidateProfile Profile { get; set; } = new();

    [Required]
    public JobSearchFilters Filters { get; set; } = new();

    [Range(1, 10)]
    public int MinScore { get; set; } = 6;

    // Ignore cached scores and score everything again, e.g. after the profile changed
    public bool ForceRescore { get; set; }
}
