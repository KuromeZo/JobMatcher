using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Persistence;

public class JobOfferEntity
{
    [Key]
    public string Guid { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string City { get; set; } = "";
    public string WorkplaceType { get; set; } = "";
    public string RequiredSkillsJson { get; set; } = "";
    public string EmploymentTypesJson { get; set; } = "";
    public string? Body { get; set; }
    public string PublishedAt { get; set; } = "";
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}