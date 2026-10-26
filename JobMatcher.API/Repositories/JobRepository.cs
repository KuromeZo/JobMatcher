using System.Text.Json;
using JobMatcher.API.Data;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.Persistence;
using JobMatcher.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobMatcher.API.Repositories;

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db)
    {
        _db = db;
    }

    // ─── Scored jobs ───────────────────────────────────────────────────────────

    public async Task<Dictionary<string, ScoredJob>> GetCachedScoresAsync(
        int userId, IReadOnlyCollection<string> guids, CancellationToken ct)
    {
        var entities = await _db.ScoredJobs
            .AsNoTracking()
            .Where(s => s.UserId == userId && guids.Contains(s.Guid))
            .ToListAsync(ct);

        return entities
            .DistinctBy(e => e.Guid)
            .ToDictionary(e => e.Guid, e => new ScoredJob
            {
                Score = e.Score,
                Matches = JsonSerializer.Deserialize<List<string>>(e.MatchesJson) ?? [],
                ToLearn = JsonSerializer.Deserialize<List<string>>(e.ToLearnJson) ?? [],
                Verdict = e.Verdict
            });
    }

    public async Task SaveScoreAsync(ScoredJob scored, int userId, CancellationToken ct)
    {
        var guid = scored.Offer.Guid;
        var entity = await _db.ScoredJobs
            .FirstOrDefaultAsync(s => s.Guid == guid && s.UserId == userId, ct);

        if (entity == null)
        {
            entity = new ScoredJobEntity { UserId = userId, Guid = guid };
            _db.ScoredJobs.Add(entity);
        }

        entity.Title = scored.Offer.Title;
        entity.CompanyName = scored.Offer.CompanyName;
        entity.Score = scored.Score;
        entity.MatchesJson = JsonSerializer.Serialize(scored.Matches);
        entity.ToLearnJson = JsonSerializer.Serialize(scored.ToLearn);
        entity.Verdict = scored.Verdict;
        entity.CachedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ─── Job offers ────────────────────────────────────────────────────────────

    public async Task<HashSet<string>> GetExistingGuidsAsync(CancellationToken ct)
    {
        var guids = await _db.JobOffers
            .Select(o => o.Guid)
            .ToListAsync(ct);

        return [..guids];
    }

    public async Task SaveOfferAsync(JobOffer offer, CancellationToken ct)
    {
        if (await _db.JobOffers.AnyAsync(o => o.Guid == offer.Guid, ct)) return;

        _db.JobOffers.Add(ToEntity(offer));
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateExpiryAsync(IReadOnlyCollection<JobOffer> offers, CancellationToken ct)
    {
        var expiryByGuid = offers
            .Where(o => o.ExpiredAt != null)
            .DistinctBy(o => o.Guid)
            .ToDictionary(o => o.Guid, o => o.ExpiredAt);

        if (expiryByGuid.Count == 0) return;

        var guids = expiryByGuid.Keys.ToList();
        var entities = await _db.JobOffers
            .Where(o => guids.Contains(o.Guid))
            .ToListAsync(ct);

        foreach (var entity in entities)
            entity.ExpiredAt = expiryByGuid[entity.Guid];

        // Only rows whose date actually changed are written
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteExpiredOffersAsync(int fallbackDaysToKeep, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var fallbackCutoff = now.AddDays(-fallbackDaysToKeep);
        var expired = _db.JobOffers.Where(o =>
            o.ExpiredAt < now || (o.ExpiredAt == null && o.FetchedAt < fallbackCutoff));
        var expiredGuids = expired.Select(o => o.Guid);

        // Set-based deletes: nothing is loaded into memory
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        await _db.ScoredJobs
            .Where(s => expiredGuids.Contains(s.Guid))
            .ExecuteDeleteAsync(ct);

        await expired.ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
    }

    public async Task<Dictionary<string, string?>> GetOfferBodiesAsync(
        IReadOnlyCollection<string> guids, CancellationToken ct)
    {
        return await _db.JobOffers
            .Where(o => guids.Contains(o.Guid))
            .ToDictionaryAsync(o => o.Guid, o => o.Body, ct);
    }

    public async Task<List<JobOffer>> GetOffersByCategoryAsync(
        List<string> categories, List<string> levels, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var entities = await _db.JobOffers
            .AsNoTracking()
            .Where(o => categories.Contains(o.Category) && levels.Contains(o.ExperienceLevel))
            .Where(o => o.ExpiredAt == null || o.ExpiredAt > now)
            .Select(o => new JobOfferEntity
            {
                Guid = o.Guid,
                Slug = o.Slug,
                Title = o.Title,
                CompanyName = o.CompanyName,
                City = o.City,
                WorkplaceType = o.WorkplaceType,
                RequiredSkillsJson = o.RequiredSkillsJson,
                EmploymentTypesJson = o.EmploymentTypesJson,
                PublishedAt = o.PublishedAt,
                FetchedAt = o.FetchedAt,
                ExpiredAt = o.ExpiredAt,
                Category = o.Category,
                ExperienceLevel = o.ExperienceLevel
                // Body is skipped on purpose: it is large and only needed for scoring
            })
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    // ─── Mapping ───────────────────────────────────────────────────────────────

    private static JobOfferEntity ToEntity(JobOffer offer) => new()
    {
        Guid = offer.Guid,
        Slug = offer.Slug,
        Title = offer.Title,
        CompanyName = offer.CompanyName,
        City = offer.City,
        WorkplaceType = offer.WorkplaceType,
        RequiredSkillsJson = JsonSerializer.Serialize(offer.RequiredSkills),
        EmploymentTypesJson = JsonSerializer.Serialize(offer.EmploymentTypes),
        Body = offer.Body,
        PublishedAt = offer.PublishedAt,
        FetchedAt = DateTime.UtcNow,
        ExpiredAt = offer.ExpiredAt,
        Category = offer.Category?.Key ?? "",
        ExperienceLevel = offer.ExperienceLevel
    };

    private static JobOffer ToDomain(JobOfferEntity entity) => new()
    {
        Guid = entity.Guid,
        Slug = entity.Slug,
        Title = entity.Title,
        CompanyName = entity.CompanyName,
        City = entity.City,
        WorkplaceType = entity.WorkplaceType,
        RequiredSkills = JsonSerializer.Deserialize<List<RequiredSkill>>(entity.RequiredSkillsJson) ?? [],
        EmploymentTypes = JsonSerializer.Deserialize<List<EmploymentType>>(entity.EmploymentTypesJson) ?? [],
        Body = entity.Body,
        PublishedAt = entity.PublishedAt,
        ExpiredAt = entity.ExpiredAt,
        Category = new JobCategory { Key = entity.Category },
        ExperienceLevel = entity.ExperienceLevel
    };
}
