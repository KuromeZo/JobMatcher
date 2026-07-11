using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IAiCvParserService
{
    Task<CandidateProfile> ParseCvAsync(string cvText);
}