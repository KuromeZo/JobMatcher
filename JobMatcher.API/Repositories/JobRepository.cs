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

    public async Task<ScoredJob?> GetCachedScoreAsync(string guid, int userId)
    {
        var entity = await _db.ScoredJobs
            .FirstOrDefaultAsync(s => s.Guid == guid && s.UserId == userId);

        if (entity == null) return null;

        return new ScoredJob
        {
            Score = entity.Score,
            Matches = JsonSerializer.Deserialize<List<string>>(entity.MatchesJson) ?? [],
            ToLearn = JsonSerializer.Deserialize<List<string>>(entity.ToLearnJson) ?? [],
            Verdict = entity.Verdict
        };
    }

    public async Task SaveScoreAsync(string guid, ScoredJob scored, int userId)
    {
        var existing = await _db.ScoredJobs
            .FirstOrDefaultAsync(s => s.Guid == guid && s.UserId == userId);

        if (existing != null) return;

        _db.ScoredJobs.Add(new ScoredJobEntity
        {
            UserId = userId,
            Guid = guid,
            Title = scored.Offer.Title,
            CompanyName = scored.Offer.CompanyName,
            Score = scored.Score,
            MatchesJson = JsonSerializer.Serialize(scored.Matches),
            ToLearnJson = JsonSerializer.Serialize(scored.ToLearn),
            Verdict = scored.Verdict,
            CachedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public async Task ClearScoresAsync(int userId)
    {
        var scores = await _db.ScoredJobs
            .Where(s => s.UserId == userId)
            .ToListAsync();

        _db.ScoredJobs.RemoveRange(scores);
        await _db.SaveChangesAsync();
    }

    // ─── Job offers ────────────────────────────────────────────────────────────

    public async Task<HashSet<string>> GetExistingGuidAsync()
    {
        var guids = await _db.JobOffers
            .Select(o => o.Guid)
            .ToListAsync();

        return [..guids];
    }

    public async Task SaveOffersAsync(IEnumerable<JobOffer> offers)
    {
        var existing = await GetExistingGuidAsync();

        var newOffers = offers
            .Where(o => !existing.Contains(o.Guid))
            .Select(o => new JobOfferEntity
            {
                Guid = o.Guid,
                Slug = o.Slug,
                Title = o.Title,
                CompanyName = o.CompanyName,
                City = o.City,
                WorkplaceType = o.WorkplaceType,
                RequiredSkillsJson = JsonSerializer.Serialize(o.RequiredSkills),
                EmploymentTypesJson = JsonSerializer.Serialize(o.EmploymentTypes),
                Body = o.Body,
                PublishedAt = o.PublishedAt,
                FetchedAt = DateTime.UtcNow
            })
            .ToList();

        if (newOffers.Count == 0) return;

        _db.JobOffers.AddRange(newOffers);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteExpiredOffersAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);

        var expiredGuids = await _db.JobOffers
            .Where(o => o.FetchedAt < cutoff)
            .Select(o => o.Guid)
            .ToListAsync();

        if (expiredGuids.Count == 0) return;

        var expiredScores = await _db.ScoredJobs
            .Where(s => expiredGuids.Contains(s.Guid))
            .ToListAsync();

        _db.ScoredJobs.RemoveRange(expiredScores);

        var expiredOffers = await _db.JobOffers
            .Where(o => expiredGuids.Contains(o.Guid))
            .ToListAsync();

        _db.JobOffers.RemoveRange(expiredOffers);

        await _db.SaveChangesAsync();
    }

    public async Task<string?> GetOfferBodyAsync(string guid)
    {
        var entity = await _db.JobOffers.FindAsync(guid);
        return entity?.Body;
    }

    public async Task<List<JobOffer>> GetAllOffersAsync()
    {
        var entities = await _db.JobOffers.ToListAsync();

        return entities.Select(e => new JobOffer
        {
            Guid = e.Guid,
            Slug = e.Slug,
            Title = e.Title,
            CompanyName = e.CompanyName,
            City = e.City,
            WorkplaceType = e.WorkplaceType,
            RequiredSkills = JsonSerializer.Deserialize<List<RequiredSkill>>(e.RequiredSkillsJson) ?? [],
            EmploymentTypes = JsonSerializer.Deserialize<List<EmploymentType>>(e.EmploymentTypesJson) ?? [],
            Body = e.Body,
            PublishedAt = e.PublishedAt
        }).ToList();
    }
    
    public async Task<List<JobOffer>> GetOffersByCategoryAsync(List<string> categories, List<string> levels)
    {
        var entities = await _db.JobOffers
            .Where(o => categories.Contains(o.Category) && levels.Contains(o.ExperienceLevel))
            .ToListAsync();

        return entities.Select(e => new JobOffer
        {
            Guid = e.Guid,
            Slug = e.Slug,
            Title = e.Title,
            CompanyName = e.CompanyName,
            City = e.City,
            WorkplaceType = e.WorkplaceType,
            RequiredSkills = JsonSerializer.Deserialize<List<RequiredSkill>>(e.RequiredSkillsJson) ?? [],
            EmploymentTypes = JsonSerializer.Deserialize<List<EmploymentType>>(e.EmploymentTypesJson) ?? [],
            Body = e.Body,
            PublishedAt = e.PublishedAt,
            Category = new JobCategory { Key = e.Category },
            ExperienceLevel = e.ExperienceLevel
        }).ToList();
    }
}