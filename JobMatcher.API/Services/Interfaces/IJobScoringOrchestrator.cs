using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobScoringOrchestrator
{
    Task<List<ScoredJob>> GetScoredJobsAsync(CandidateProfile? profile = null);
}