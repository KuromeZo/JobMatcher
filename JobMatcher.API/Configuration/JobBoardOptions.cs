using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Configuration;

public class JobBoardOptions
{
    public const string SectionName = "JustJoinIt";

    [Required, Url]
    public string BaseUrl { get; set; } = "https://justjoin.it/api/candidate-api/";
}
