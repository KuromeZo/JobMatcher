using JobMatcher.API.Models.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobMatcher.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ScoredJobEntity> ScoredJobs { get; set; }
    public DbSet<JobOfferEntity> JobOffers { get; set; }
    public DbSet<UserEntity> Users { get; set; }
}