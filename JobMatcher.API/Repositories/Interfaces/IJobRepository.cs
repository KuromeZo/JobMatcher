using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Repositories.Interfaces;

public interface IJobRepository
{
    // Scored jobs
    Task<ScoredJob?> GetCachedScoreAsync(string guid);
    Task SaveScoreAsync(string guid, ScoredJob scored);

    // Job offers
    Task<HashSet<string>> GetExistingGuidAsync();
    Task SaveOffersAsync(IEnumerable<JobOffer> offers);
    Task DeleteExpiredOffersAsync(int daysToKeep = 30);
    
    Task<string?> GetOfferBodyAsync(string guid);
}