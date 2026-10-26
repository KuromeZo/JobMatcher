using System.Runtime.CompilerServices;
using System.Threading.Channels;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Repositories.Interfaces;
using JobMatcher.API.Services.Interfaces;

namespace JobMatcher.API.Services;

public class JobScoringOrchestrator : IJobScoringOrchestrator
{
    private const int OfferRetentionDays = 30;
    private const int MaxParallelScoring = 4;
    private static readonly TimeSpan JobBoardRequestDelay = TimeSpan.FromMilliseconds(300);

    private readonly IJobFetcherService _fetcher;
    private readonly IAiScoringService _scorer;
    private readonly IJobRepository _repository;
    private readonly ILogger<JobScoringOrchestrator> _logger;

    public JobScoringOrchestrator(
        IJobFetcherService fetcher,
        IAiScoringService scorer,
        IJobRepository repository,
        ILogger<JobScoringOrchestrator> logger)
    {
        _fetcher = fetcher;
        _scorer = scorer;
        _repository = repository;
        _logger = logger;
    }

    public async IAsyncEnumerable<ScoredJob> StreamScoredJobsAsync(
        int userId, ScoreRequest request, [EnumeratorCancellation] CancellationToken ct)
    {
        await _repository.DeleteExpiredOffersAsync(OfferRetentionDays, ct);

        var knownGuids = await _repository.GetExistingGuidsAsync(ct);
        var offers = await CollectOffersAsync(request.Filters, knownGuids, ct);

        var cachedScores = request.ForceRescore
            ? new Dictionary<string, ScoredJob>()
            : await _repository.GetCachedScoresAsync(userId, offers.Select(o => o.Guid).ToList(), ct);

        // 1. Cached scores cost nothing, so they go out right away
        var cachedResults = offers
            .Where(o => cachedScores.ContainsKey(o.Guid))
            .Select(o =>
            {
                var cached = cachedScores[o.Guid];
                cached.Offer = o;
                return cached;
            })
            .Where(s => s.Score >= request.MinScore)
            .OrderByDescending(s => s.Score);

        foreach (var cached in cachedResults)
            yield return cached;

        // 2. Everything else is scored by the AI
        var toScore = offers.Where(o => !cachedScores.ContainsKey(o.Guid)).ToList();

        _logger.LogInformation(
            "User {UserId}: {Total} offers, {Cached} cached, {ToScore} to score ({New} new)",
            userId, offers.Count, cachedScores.Count, toScore.Count,
            toScore.Count(o => !knownGuids.Contains(o.Guid)));

        if (toScore.Count == 0) yield break;

        // Known offers already have a body in the DB; new ones are fetched from the job board while scoring
        var storedBodies = await _repository.GetOfferBodiesAsync(
            toScore.Where(o => knownGuids.Contains(o.Guid)).Select(o => o.Guid).ToList(), ct);

        foreach (var offer in toScore)
        {
            if (storedBodies.TryGetValue(offer.Guid, out var body))
                offer.Body = body;
        }

        await foreach (var (offer, scored) in ScoreInParallelAsync(toScore, knownGuids, request.Profile, ct))
        {
            // DbContext is not thread-safe, so results are saved here, one at a time.
            // Saving right away also keeps finished work if the client disconnects mid-stream.
            if (!knownGuids.Contains(offer.Guid) && offer.Body != null)
                await _repository.SaveOfferAsync(offer, ct);

            if (scored == null) continue;

            await _repository.SaveScoreAsync(scored, userId, ct);

            // The body is only needed for scoring; no reason to send kilobytes of HTML to the client
            offer.Body = null;

            if (scored.Score >= request.MinScore)
                yield return scored;
        }
    }

    // The job board stops paging on a streak of known offers, so older matching offers come from the DB
    private async Task<List<JobOffer>> CollectOffersAsync(
        JobSearchFilters filters, HashSet<string> knownGuids, CancellationToken ct)
    {
        var offers = await _fetcher.FetchOffersMetadataAsync(knownGuids, filters, ct);
        var fetchedGuids = offers.Select(o => o.Guid).ToHashSet();

        var stored = await _repository.GetOffersByCategoryAsync(filters.Categories, filters.ExperienceLevels, ct);
        offers.AddRange(stored.Where(o => !fetchedGuids.Contains(o.Guid)));

        return offers;
    }

    /// <summary>
    /// Scores offers with limited parallelism and yields each result as soon as it is ready.
    /// Workers only call HTTP services; the caller does all DB work.
    /// </summary>
    private async IAsyncEnumerable<(JobOffer Offer, ScoredJob? Scored)> ScoreInParallelAsync(
        List<JobOffer> offers, HashSet<string> knownGuids, CandidateProfile profile,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<(JobOffer, ScoredJob?)>();
        using var workersCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var workers = RunWorkersAsync();

        try
        {
            await foreach (var result in channel.Reader.ReadAllAsync(ct))
                yield return result;
        }
        finally
        {
            // Stops the workers if the consumer stopped early (client disconnected or an error)
            await workersCts.CancelAsync();
            await workers;
        }

        async Task RunWorkersAsync()
        {
            try
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxParallelScoring,
                    CancellationToken = workersCts.Token
                };

                await Parallel.ForEachAsync(offers, options, async (offer, token) =>
                {
                    var scored = await ScoreOfferAsync(offer, isKnown: knownGuids.Contains(offer.Guid), profile, token);
                    await channel.Writer.WriteAsync((offer, scored), token);
                });

                channel.Writer.Complete();
            }
            catch (Exception ex)
            {
                channel.Writer.Complete(ex);
            }
        }
    }

    private async Task<ScoredJob?> ScoreOfferAsync(
        JobOffer offer, bool isKnown, CandidateProfile profile, CancellationToken ct)
    {
        if (!isKnown)
        {
            offer.Body = await _fetcher.FetchOfferBodyAsync(offer.Slug, ct);
            await Task.Delay(JobBoardRequestDelay, ct);

            // Not scored and not saved, so it is retried on the next run
            if (offer.Body == null) return null;
        }

        return await _scorer.ScoreJobAsync(offer, profile, ct);
    }
}
