using System.Text.Json;
using JobMatcher.API.Data;
using JobMatcher.API.Models;
using Microsoft.EntityFrameworkCore;

namespace JobMatcher.API.Repositories;

public class JobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ScoredJob?> GetCachedAsync(string guid)
    {
        var entity = await _db.ScoredJobs.FindAsync(guid);
        if (entity == null) return null;

        return new ScoredJob
        {
            Score = entity.Score,
            Matches = JsonSerializer.Deserialize<List<string>>(entity.MatchesJson) ?? [],
            ToLearn = JsonSerializer.Deserialize<List<string>>(entity.ToLearnJson) ?? [],
            Verdict = entity.Verdict
        };
    }

    public async Task SaveAsync(string guid, ScoredJob scored)
    {
        var existing = await _db.ScoredJobs.FindAsync(guid);
        if (existing != null) return; // уже есть

        _db.ScoredJobs.Add(new ScoredJobEntity
        {
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
}