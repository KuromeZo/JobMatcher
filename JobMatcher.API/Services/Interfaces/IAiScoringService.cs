using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IAiScoringService
{
    /// <summary>Returns null when the offer could not be scored.</summary>
    Task<ScoredJob?> ScoreJobAsync(JobOffer offer, CandidateProfile profile, CancellationToken ct);
}
