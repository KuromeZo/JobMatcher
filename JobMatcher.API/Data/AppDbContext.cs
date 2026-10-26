using JobMatcher.API.Models.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobMatcher.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ScoredJobEntity> ScoredJobs { get; set; }
    public DbSet<JobOfferEntity> JobOffers { get; set; }
    public DbSet<UserEntity> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One score per user and offer; also serves the cache lookup
        modelBuilder.Entity<ScoredJobEntity>()
            .HasIndex(s => new { s.UserId, s.Guid })
            .IsUnique();

        // Cleanup of expired scores looks them up by offer
        modelBuilder.Entity<ScoredJobEntity>()
            .HasIndex(s => s.Guid);

        // Protects against two concurrent registrations with the same login
        modelBuilder.Entity<UserEntity>()
            .HasIndex(u => u.Login)
            .IsUnique();

        modelBuilder.Entity<JobOfferEntity>()
            .HasIndex(o => o.FetchedAt);

        modelBuilder.Entity<JobOfferEntity>()
            .HasIndex(o => new { o.Category, o.ExperienceLevel });
    }
}
