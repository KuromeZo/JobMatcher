using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Domain;

public class JobSearchFilters : IValidatableObject
{
    // Values are passed to the JustJoinIT query string, so only known ones are accepted
    public static readonly IReadOnlySet<string> AllowedCategories =
        new HashSet<string> { "net", "java", "javascript", "data", "python", "devops" };

    public static IReadOnlySet<string> AllowedLevels => CandidateProfile.Levels;

    [MinLength(1)]
    public List<string> Categories { get; set; } = [];

    [MinLength(1)]
    public List<string> ExperienceLevels { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var unknownCategories = Categories.Where(c => !AllowedCategories.Contains(c)).ToList();
        if (unknownCategories.Count > 0)
            yield return new ValidationResult(
                $"Unknown categories: {string.Join(", ", unknownCategories)}", [nameof(Categories)]);

        var unknownLevels = ExperienceLevels.Where(l => !AllowedLevels.Contains(l)).ToList();
        if (unknownLevels.Count > 0)
            yield return new ValidationResult(
                $"Unknown experience levels: {string.Join(", ", unknownLevels)}", [nameof(ExperienceLevels)]);
    }
}
