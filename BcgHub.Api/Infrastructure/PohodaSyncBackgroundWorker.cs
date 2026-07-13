using BcgHub.Api.Application;
using Microsoft.Extensions.Options;

namespace BcgHub.Api.Infrastructure;

public sealed class PohodaSyncBackgroundWorker(IPohodaSyncService sync, IOptions<PohodaOptions> options, ILogger<PohodaSyncBackgroundWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled) { logger.LogInformation("Automatic POHODA synchronization is disabled."); return; }
        var interval = TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes));
        logger.LogInformation("Automatic POHODA synchronization worker started. Interval: {IntervalMinutes} minutes, endpoint: {Endpoint}, company number: {CompanyNumber}.", interval.TotalMinutes, SafeEndpoint(settings.BaseUrl), settings.CompanyNumber);
        await RunAsync("startup", stoppingToken);
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RunAsync("scheduled", stoppingToken);
    }

    private async Task RunAsync(string trigger, CancellationToken cancellationToken)
    {
        try { await sync.SyncAsync(trigger, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { logger.LogWarning("Automatic POHODA synchronization trigger {Trigger} finished with an error already recorded by the synchronization service: {Error}", trigger, exception.Message); }
    }

    private static string SafeEndpoint(string baseUrl) => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : "invalid";
}
