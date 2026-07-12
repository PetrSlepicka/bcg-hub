using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BcgHub.Api.Infrastructure;

public sealed class BcgHubDbContextFactory : IDesignTimeDbContextFactory<BcgHubDbContext>
{
    public BcgHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "Host=localhost;Port=5432;Database=bcg_hub;Username=postgres;Password=postgres";
        return new BcgHubDbContext(new DbContextOptionsBuilder<BcgHubDbContext>().UseNpgsql(connectionString).Options);
    }
}
