using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Repositories.Interfaces;

public interface IJobRepository
{
    // Scored jobs

    /// <summary>Cached scores of the user for the given offers, keyed by offer guid. Offers are not filled in.</summary>
    Task<Dictionary<string, ScoredJob>> GetCachedScoresAsync(int userId, IReadOnlyCollection<string> guids, CancellationToken ct);

    /// <summary>Inserts the score or overwrites the existing one for the same user and offer.</summary>
    Task SaveScoreAsync(ScoredJob scored, int userId, CancellationToken ct);

    // Job offers
    Task<HashSet<string>> GetExistingGuidsAsync(CancellationToken ct);
    Task SaveOfferAsync(JobOffer offer, CancellationToken ct);
    Task DeleteExpiredOffersAsync(int daysToKeep, CancellationToken ct);
    Task<Dictionary<string, string?>> GetOfferBodiesAsync(IReadOnlyCollection<string> guids, CancellationToken ct);

    /// <summary>Offers matching the filters, without bodies.</summary>
    Task<List<JobOffer>> GetOffersByCategoryAsync(List<string> categories, List<string> levels, CancellationToken ct);
}
