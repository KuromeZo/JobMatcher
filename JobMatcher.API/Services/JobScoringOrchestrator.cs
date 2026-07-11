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

    public async Task<List<ScoredJob>> GetScoredJobsAsync(int userId, ScoreRequest? request = null)
    {
        var profile = request?.Profile ?? BuildProfile();
        var filters = request?.Filters ?? new JobSearchFilters();
        var minScore = request?.MinScore ?? 6;

        // Если пользователь сменил профиль — сбрасываем его оценки
        if (request?.ForceRescore == true)
        {
            _logger.LogInformation("ForceRescore — clearing scores for user {UserId}", userId);
            await _repository.ClearScoresAsync(userId);
        }

        // 1. Удаляем просроченные офферы
        await _repository.DeleteExpiredOffersAsync();

        // 2. Получаем существующие guids из БД
        var existingGuids = await _repository.GetExistingGuidAsync();

        // 3. Фетчим метаданные с JustJoinIT — останавливаемся при стрике из 5 известных
        var fetchedOffers = await _fetcher.FetchOffersMetadataAsync(existingGuids, filters);

        // 4. Фетчим body только для новых офферов
        var newOffers = fetchedOffers
            .Where(o => !existingGuids.Contains(o.Guid))
            .ToList();

        _logger.LogInformation("Found {New} new offers out of {Total} fetched",
            newOffers.Count, fetchedOffers.Count);

        foreach (var offer in newOffers)
        {
            offer.Body = await _fetcher.FetchOfferBodyAsync(offer.Slug);
            await Task.Delay(300);
        }

        await _repository.SaveOffersAsync(newOffers);

        // 5. Скорим все офферы — новые через Claude, старые из кеша
        var allOffersForScoring = fetchedOffers.Count > 0
            ? fetchedOffers
            : await _repository.GetAllOffersAsync();

        var scoredJobs = new List<ScoredJob>();

        foreach (var offer in allOffersForScoring)
        {
            var cached = await _repository.GetCachedScoreAsync(offer.Guid, userId);
            if (cached != null)
            {
                offer.Body = await _repository.GetOfferBodyAsync(offer.Guid);
                cached.Offer = offer;
                if (cached.Score >= minScore)
                    scoredJobs.Add(cached);
                continue;
            }

            var scored = await _scorer.ScoreJobAsync(offer, profile);
            if (scored != null)
            {
                await _repository.SaveScoreAsync(offer.Guid, scored, userId);
                if (scored.Score >= minScore)
                    scoredJobs.Add(scored);
            }
        }

        return scoredJobs
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    public async IAsyncEnumerable<ScoredJob> GetScoredJobsStreamAsync(int userId, ScoreRequest? request = null)
    {
        var profile = request?.Profile ?? BuildProfile();
        var filters = request?.Filters ?? new JobSearchFilters();
        var minScore = request?.MinScore ?? 6;

        // Если пользователь сменил профиль — сбрасываем его оценки
        if (request?.ForceRescore == true)
        {
            _logger.LogInformation("ForceRescore — clearing scores for user {UserId}", userId);
            await _repository.ClearScoresAsync(userId);
        }

        // 1. Удаляем просроченные офферы
        await _repository.DeleteExpiredOffersAsync();

        // 2. Получаем существующие guids из БД
        var existingGuids = await _repository.GetExistingGuidAsync();

        // 3. Фетчим метаданные с JustJoinIT
        var fetchedOffers = await _fetcher.FetchOffersMetadataAsync(existingGuids, filters);

        var newOffers = fetchedOffers
            .Where(o => !existingGuids.Contains(o.Guid))
            .ToList();

        _logger.LogInformation("Found {New} new offers out of {Total} fetched",
            newOffers.Count, fetchedOffers.Count);

        // 4. Для новых — фетчим body и сразу скорим → yield
        foreach (var offer in newOffers)
        {
            _logger.LogInformation("Fetching body and scoring: {Title}", offer.Title);
            offer.Body = await _fetcher.FetchOfferBodyAsync(offer.Slug);
            await Task.Delay(300);

            var scored = await _scorer.ScoreJobAsync(offer, profile);
            if (scored != null)
            {
                await _repository.SaveScoreAsync(offer.Guid, scored, userId);
                if (scored.Score >= minScore)
                    yield return scored;
            }
        }

        // 5. Для уже известных — берём из кеша и сразу отдаём
        var cachedOffers = fetchedOffers.Count > 0
            ? fetchedOffers.Where(o => existingGuids.Contains(o.Guid)).ToList()
            : await _repository.GetAllOffersAsync();
        
        _logger.LogInformation("CachedOffers count: {Count}", cachedOffers.Count);

        foreach (var offer in cachedOffers)
        {
            var cached = await _repository.GetCachedScoreAsync(offer.Guid, userId);
            if (cached != null)
            {
                offer.Body = await _repository.GetOfferBodyAsync(offer.Guid);
                cached.Offer = offer;
                if (cached.Score >= minScore)
                    yield return cached;
            }
            else
            {
                // Нет оценки для этого пользователя — оцениваем
                offer.Body = await _repository.GetOfferBodyAsync(offer.Guid);
                var scored = await _scorer.ScoreJobAsync(offer, profile);
                if (scored != null)
                {
                    await _repository.SaveScoreAsync(offer.Guid, scored, userId);
                    if (scored.Score >= minScore)
                        yield return scored;
                }
            }
        }

        // 6. Сохраняем новые офферы в БД
        await _repository.SaveOffersAsync(newOffers);
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