using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobScoringOrchestrator
{
    Task<List<ScoredJob>> GetScoredJobsAsync(int userId, ScoreRequest? request = null);
    IAsyncEnumerable<ScoredJob> GetScoredJobsStreamAsync(int userId, ScoreRequest? request = null);
}