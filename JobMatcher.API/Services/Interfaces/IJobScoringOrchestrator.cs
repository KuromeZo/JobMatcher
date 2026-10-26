using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IJobScoringOrchestrator
{
    /// <summary>
    /// Yields cached scores first (best first), then newly scored offers as soon as each one is ready.
    /// Only jobs with a score of at least <see cref="ScoreRequest.MinScore"/> are returned.
    /// </summary>
    IAsyncEnumerable<ScoredJob> StreamScoredJobsAsync(int userId, ScoreRequest request, CancellationToken ct);
}
