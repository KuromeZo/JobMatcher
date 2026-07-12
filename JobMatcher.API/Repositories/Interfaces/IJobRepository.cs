using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.Persistence;

namespace JobMatcher.API.Repositories.Interfaces;

public interface IJobRepository
{
    // Scored jobs
    Task<ScoredJob?> GetCachedScoreAsync(string guid, int userId);
    Task SaveScoreAsync(string guid, ScoredJob scored, int userId);
    Task ClearScoresAsync(int userId);

    // Job offers
    Task<HashSet<string>> GetExistingGuidAsync();
    Task SaveOffersAsync(IEnumerable<JobOffer> offers);
    Task DeleteExpiredOffersAsync(int daysToKeep = 30);
    Task<string?> GetOfferBodyAsync(string guid);
    Task<List<JobOffer>> GetAllOffersAsync();
    Task<List<JobOffer>> GetOffersByCategoryAsync(List<string> categories, List<string> levels);
}