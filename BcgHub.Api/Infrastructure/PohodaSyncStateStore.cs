using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed record PohodaSyncStateSnapshot(DateTime? LastAttemptStartedAtUtc, DateTime? LastAttemptCompletedAtUtc, DateTime? LastSuccessfulSyncUtc, string? LastRunId, string? LastTrigger, string? LastError, int LastImportedCount, int LastUpdatedCount, int LastUnchangedCount, int LastWarningCount, int LastErrorCount);

public interface IPohodaSyncStateStore
{
    Task<PohodaSyncStateSnapshot> GetAsync(CancellationToken cancellationToken);
    Task RecordAttemptAsync(string runId, string trigger, DateTime startedAtUtc, CancellationToken cancellationToken);
    Task RecordSuccessAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, PohodaImportResult result, CancellationToken cancellationToken);
    Task RecordFailureAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, Exception exception, CancellationToken cancellationToken);
}

public sealed class PohodaSyncStateStore(IServiceScopeFactory scopeFactory) : IPohodaSyncStateStore
{
    private static readonly Guid StateId = Guid.Parse("23e932ed-42c6-4e96-9568-d7350ecb8e01");

    public async Task<PohodaSyncStateSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var state = await scope.ServiceProvider.GetRequiredService<BcgHubDbContext>().PohodaSyncStates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == StateId, cancellationToken);
        return state is null ? new PohodaSyncStateSnapshot(null, null, null, null, null, null, 0, 0, 0, 0, 0) : Snapshot(state);
    }

    public Task RecordAttemptAsync(string runId, string trigger, DateTime startedAtUtc, CancellationToken cancellationToken) => UpdateAsync(state => { state.LastRunId = runId; state.LastTrigger = trigger; state.LastAttemptStartedAtUtc = startedAtUtc; state.LastAttemptCompletedAtUtc = null; state.LastError = null; }, cancellationToken);

    public Task RecordSuccessAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, PohodaImportResult result, CancellationToken cancellationToken) => UpdateAsync(state => { state.LastRunId = runId; state.LastTrigger = trigger; state.LastAttemptStartedAtUtc = startedAtUtc; state.LastAttemptCompletedAtUtc = completedAtUtc; state.LastSuccessfulSyncUtc = startedAtUtc; state.LastError = null; state.LastImportedCount = result.ImportedCount; state.LastUpdatedCount = result.UpdatedCount; state.LastUnchangedCount = result.UnchangedCount; state.LastWarningCount = result.WarningCount; state.LastErrorCount = result.ErrorCount; }, cancellationToken);

    public Task RecordFailureAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, Exception exception, CancellationToken cancellationToken) => UpdateAsync(state => { state.LastRunId = runId; state.LastTrigger = trigger; state.LastAttemptStartedAtUtc = startedAtUtc; state.LastAttemptCompletedAtUtc = completedAtUtc; state.LastError = Limit(exception.Message, 4000); }, cancellationToken);

    private async Task UpdateAsync(Action<PohodaSyncState> update, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BcgHubDbContext>();
        var state = await db.PohodaSyncStates.SingleOrDefaultAsync(x => x.Id == StateId, cancellationToken);
        if (state is null) { state = new PohodaSyncState { Id = StateId }; db.PohodaSyncStates.Add(state); }
        update(state);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PohodaSyncStateSnapshot Snapshot(PohodaSyncState state) => new(state.LastAttemptStartedAtUtc, state.LastAttemptCompletedAtUtc, state.LastSuccessfulSyncUtc, state.LastRunId, state.LastTrigger, state.LastError, state.LastImportedCount, state.LastUpdatedCount, state.LastUnchangedCount, state.LastWarningCount, state.LastErrorCount);
    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
