namespace JobMatcher.API.Models.Domain;

public class CandidateProfile
{
    public static readonly IReadOnlySet<string> Levels = new HashSet<string> { "intern", "junior", "mid", "senior" };
    public const string DefaultLevel = "junior";

    public List<string> Skills { get; set; } = [];
    public string Level { get; set; } = DefaultLevel;
    public string Description { get; set; } = "";
}
