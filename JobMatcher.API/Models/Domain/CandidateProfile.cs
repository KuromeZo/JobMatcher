using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Domain;

public class CandidateProfile
{
    public static readonly IReadOnlySet<string> Levels = new HashSet<string> { "intern", "junior", "mid", "senior" };
    public const string DefaultLevel = "junior";

    [MinLength(1, ErrorMessage = "Profile must contain at least one skill")]
    [MaxLength(100)]
    public List<string> Skills { get; set; } = [];

    public string Level { get; set; } = DefaultLevel;

    [MaxLength(1000)]
    public string Description { get; set; } = "";
}
