using JobMatcher.API.Models.Domain;

namespace JobMatcher.API.Services.Interfaces;

public interface IAiScoringService
{
    Task<ScoredJob?> ScoreJobAsync(JobOffer offer, CandidateProfile profile);
}