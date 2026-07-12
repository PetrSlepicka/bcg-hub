using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BcgHub.Api.Infrastructure;

public sealed class BcgHubDbContextFactory : IDesignTimeDbContextFactory<BcgHubDbContext>
{
    public BcgHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection before running EF tools.");
        return new BcgHubDbContext(new DbContextOptionsBuilder<BcgHubDbContext>().UseNpgsql(connectionString).Options);
    }
}
