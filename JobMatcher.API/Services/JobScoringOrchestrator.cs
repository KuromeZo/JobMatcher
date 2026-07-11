using JobMatcher.API.Models.Domain;
using JobMatcher.API.Repositories.Interfaces;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services;

public class JobScoringOrchestrator : IJobScoringOrchestrator
{
    private readonly IJobFetcherService _fetcher;
    private readonly IAiScoringService _scorer;
    private readonly IJobRepository _repository;
    private readonly IConfiguration _config;
    private readonly ILogger<JobScoringOrchestrator> _logger;

    public JobScoringOrchestrator(
        IJobFetcherService fetcher,
        IAiScoringService scorer,
        IJobRepository repository,
        IConfiguration config,
        ILogger<JobScoringOrchestrator> logger)
    {
        _fetcher = fetcher;
        _scorer = scorer;
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    public async Task<List<ScoredJob>> GetScoredJobsAsync()
    {
        var profile = BuildProfile();
        var offers = await _fetcher.FetchJuniorOffersAsync();
        var scoredJobs = new List<ScoredJob>();

        foreach (var offer in offers)
        {
            var cached = await _repository.GetCachedAsync(offer.Guid);
            if (cached != null)
            {
                cached.Offer = offer;
                if (cached.Score >= 6)
                    scoredJobs.Add(cached);
                continue;
            }

            var scored = await _scorer.ScoreJobAsync(offer, profile);
            if (scored != null)
            {
                await _repository.SaveAsync(offer.Guid, scored);
                if (scored.Score >= 6)
                    scoredJobs.Add(scored);
            }
        }

        return scoredJobs
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    private CandidateProfile BuildProfile()
    {
        return new CandidateProfile
        {
            Skills = _config.GetSection("CandidateProfile:Skills").Get<List<string>>() ?? [],
            Level = _config["CandidateProfile:Level"] ?? "junior",
            Description = _config["CandidateProfile:Description"] ?? ""
        };
    }
}