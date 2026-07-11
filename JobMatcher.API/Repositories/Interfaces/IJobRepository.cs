using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Repositories.Interfaces;

public interface IJobRepository
{
    Task<ScoredJob?> GetCachedAsync(string guid);
    Task SaveAsync(string guid, ScoredJob scored);
}