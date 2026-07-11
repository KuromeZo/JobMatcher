namespace JobMatcher.API.Models.Domain;

public class JobOffer
{
    public string Guid { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string City { get; set; } = "";
    public string WorkplaceType { get; set; } = "";
    public string ExperienceLevel { get; set; } = "";
    public JobCategory? Category { get; set; }
    public List<RequiredSkill> RequiredSkills { get; set; } = [];
    public List<RequiredSkill> NiceToHaveSkills { get; set; } = [];
    public List<EmploymentType> EmploymentTypes { get; set; } = [];
    public string PublishedAt { get; set; } = "";
    public string? Body { get; set; }
}

public class RequiredSkill
{
    public string Name { get; set; } = "";
    public int Level { get; set; }
}

public class EmploymentType
{
    public decimal? From { get; set; }
    public decimal? To { get; set; }
    public string Currency { get; set; } = "";
    public string Type { get; set; } = "";
    public string Unit { get; set; } = "";
}

public class JobCategory
{
    public string Key { get; set; } = "";
    public string? ParentKey { get; set; }
}