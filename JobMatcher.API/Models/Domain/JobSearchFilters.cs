namespace JobMatcher.API.Models.Domain;

public class JobSearchFilters
{
    public static readonly List<string> DefaultCategories = ["net", "java", "data", "javascript"];
    public static readonly List<string> DefaultLevels = ["junior"];

    public List<string> Categories { get; set; } = DefaultCategories;
    public List<string> ExperienceLevels { get; set; } = DefaultLevels;
}