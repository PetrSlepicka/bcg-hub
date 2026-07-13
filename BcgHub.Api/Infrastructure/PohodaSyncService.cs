using System.Diagnostics;
using BcgHub.Api.Application;
using Microsoft.Extensions.Options;

namespace BcgHub.Api.Infrastructure;

public sealed class PohodaSyncService(IServiceScopeFactory scopeFactory, IPohodaMServerClient client, IPohodaSyncStateStore stateStore, IPohodaSyncLock syncLock, IOptions<PohodaOptions> options, ILogger<PohodaSyncService> logger) : IPohodaSyncService
{
    public async Task<PohodaSyncResult> SyncAsync(string trigger, CancellationToken cancellationToken)
    {
        await using var syncHandle = await syncLock.AcquireAsync(cancellationToken);
        var settings = options.Value;
        if (!settings.Enabled) throw new DomainValidationException("Automatická synchronizace POHODA není povolena.");
        var runId = Guid.NewGuid().ToString("N");
        var startedAtUtc = DateTime.UtcNow;
        var state = await stateStore.GetAsync(cancellationToken);
        var changedSinceUtc = (state.LastSuccessfulSyncUtc ?? startedAtUtc.AddDays(-Math.Clamp(settings.InitialLookbackDays, 1, 3650))).AddMinutes(-Math.Clamp(settings.OverlapMinutes, 0, 60));
        await stateStore.RecordAttemptAsync(runId, trigger, startedAtUtc, cancellationToken);
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("POHODA synchronization {RunId} started. Trigger: {Trigger}, checkpoint UTC: {CheckpointUtc}, endpoint: {Endpoint}.", runId, trigger, changedSinceUtc, SafeEndpoint(settings.BaseUrl));
        try
        {
            await using var response = await client.DownloadChangedOrdersAsync(changedSinceUtc, runId, cancellationToken);
            await using var scope = scopeFactory.CreateAsyncScope();
            var import = await scope.ServiceProvider.GetRequiredService<IPohodaOrderImportService>().ImportMServerResponseAsync(response.Content, settings.CompanyNumber, cancellationToken);
            var completedAtUtc = DateTime.UtcNow;
            await stateStore.RecordSuccessAsync(runId, trigger, startedAtUtc, completedAtUtc, import, cancellationToken);
            var result = new PohodaSyncResult(runId, trigger, changedSinceUtc, startedAtUtc, completedAtUtc, import.ImportedCount, import.UpdatedCount, import.UnchangedCount, import.WarningCount, import.ErrorCount);
            logger.LogInformation("POHODA synchronization {RunId} completed in {ElapsedMs} ms. Response bytes: {ContentLength}, imported: {ImportedCount}, updated: {UpdatedCount}, unchanged: {UnchangedCount}, warnings: {WarningCount}, errors: {ErrorCount}, next checkpoint UTC: {NextCheckpointUtc}.", runId, stopwatch.ElapsedMilliseconds, response.ContentLength, result.ImportedCount, result.UpdatedCount, result.UnchangedCount, result.WarningCount, result.ErrorCount, startedAtUtc);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("POHODA synchronization {RunId} was cancelled after {ElapsedMs} ms.", runId, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            var completedAtUtc = DateTime.UtcNow;
            try { await stateStore.RecordFailureAsync(runId, trigger, startedAtUtc, completedAtUtc, exception, cancellationToken); }
            catch (Exception stateException) { logger.LogError(stateException, "POHODA synchronization {RunId} failed to persist its failure state.", runId); }
            logger.LogError(exception, "POHODA synchronization {RunId} failed after {ElapsedMs} ms. Trigger: {Trigger}, checkpoint UTC: {CheckpointUtc}, endpoint: {Endpoint}.", runId, stopwatch.ElapsedMilliseconds, trigger, changedSinceUtc, SafeEndpoint(settings.BaseUrl));
            throw;
        }
    }

    public async Task<PohodaSyncStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var state = await stateStore.GetAsync(cancellationToken);
        return new PohodaSyncStatus(settings.Enabled, SafeEndpoint(settings.BaseUrl), Math.Max(1, settings.IntervalMinutes), state.LastAttemptStartedAtUtc, state.LastAttemptCompletedAtUtc, state.LastSuccessfulSyncUtc, state.LastRunId, state.LastTrigger, state.LastError, state.LastImportedCount, state.LastUpdatedCount, state.LastUnchangedCount, state.LastWarningCount, state.LastErrorCount);
    }

    private static string SafeEndpoint(string baseUrl) => Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : "invalid";
}
