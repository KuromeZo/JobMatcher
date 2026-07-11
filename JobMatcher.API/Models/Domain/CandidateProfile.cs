namespace JobMatcher.API.Models.Domain;

public class CandidateProfile
{
    public List<string> Skills { get; set; } = [];
    public string Level { get; set; } = "junior";
    public string Description { get; set; } = "";
}