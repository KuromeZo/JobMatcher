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

    public async Task<List<ScoredJob>> GetScoredJobsAsync(CandidateProfile? profile = null)
    {
        profile ??= BuildProfile();

        // 1. Удаляем просроченные офферы
        await _repository.DeleteExpiredOffersAsync();

        // 2. Получаем существующие guids из БД
        var existingGuids = await _repository.GetExistingGuidAsync();

        // 3. Фетчим метаданные с JustJoinIT — останавливаемся при стрике из 5 известных
        var fetchedOffers = await _fetcher.FetchOffersMetadataAsync(existingGuids);

        // 4. Фетчим body только для новых офферов и сохраняем в БД
        var newOffers = fetchedOffers
            .Where(o => !existingGuids.Contains(o.Guid))
            .ToList();

        _logger.LogInformation("Found {New} new offers out of {Total} fetched",
            newOffers.Count, fetchedOffers.Count);

        foreach (var offer in newOffers)
        {
            _logger.LogInformation("Fetching body for new offer: {Title}", offer.Title);
            offer.Body = await _fetcher.FetchOfferBodyAsync(offer.Slug);
            await Task.Delay(300);
        }

        await _repository.SaveOffersAsync(newOffers);

        // 5. Скорим все офферы — новые через Claude, старые из кеша
        var scoredJobs = new List<ScoredJob>();

        foreach (var offer in fetchedOffers)
        {
            var cached = await _repository.GetCachedScoreAsync(offer.Guid);
            if (cached != null)
            {
                offer.Body = await _repository.GetOfferBodyAsync(offer.Guid);
                cached.Offer = offer;
                if (cached.Score >= 6)
                    scoredJobs.Add(cached);
                continue;
            }

            var scored = await _scorer.ScoreJobAsync(offer, profile);
            if (scored != null)
            {
                await _repository.SaveScoreAsync(offer.Guid, scored);
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