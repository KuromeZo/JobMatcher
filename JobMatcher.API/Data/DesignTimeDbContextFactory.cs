using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobMatcher.API.Data;

/// <summary>
/// Used only by `dotnet ef` tooling, so creating migrations doesn't need real secrets or a running database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=jobmatcher_design_time")
            .Options;

        return new AppDbContext(options);
    }
}
