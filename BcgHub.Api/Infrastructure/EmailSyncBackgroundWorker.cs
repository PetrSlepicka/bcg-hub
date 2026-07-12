using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSyncBackgroundWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EmailSyncBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("EmailSync:IntervalMinutes", 5)));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await SyncAllAsync(stoppingToken);
    }

    private async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BcgHubDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<EmailSyncService>();
        var userIds = await db.EmailAccountSettings.AsNoTracking().Where(x => x.IsActive).Select(x => x.UserAccountId).ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            try { await service.SyncUserAsync(userId, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception) { logger.LogError(exception, "Automatická synchronizace schránky uživatele {UserId} selhala.", userId); }
        }
    }
}
