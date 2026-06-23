using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models;

public class ScoredJobEntity
{
    [Key]
    public string Guid { get; set; } = "";
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public int Score { get; set; }
    public string MatchesJson { get; set; } = "";
    public string ToLearnJson { get; set; } = "";
    public string Verdict { get; set; } = "";
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}