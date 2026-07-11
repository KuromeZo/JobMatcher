using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Models.External.JustJoinIt;

public class JustJoinItResponse
{
    public List<JobOffer> Data { get; set; } = [];
    public Meta Meta { get; set; } = new();
}

public class Meta
{
    public int TotalItems { get; set; }
    public NextPage? Next { get; set; }
}

public class NextPage
{
    public int? Cursor { get; set; }
}