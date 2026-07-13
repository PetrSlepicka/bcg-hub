namespace BcgHub.Api.Domain;

public sealed class PohodaSyncState : Entity
{
    public DateTime? LastAttemptStartedAtUtc { get; set; }
    public DateTime? LastAttemptCompletedAtUtc { get; set; }
    public DateTime? LastSuccessfulSyncUtc { get; set; }
    public string? LastRunId { get; set; }
    public string? LastTrigger { get; set; }
    public string? LastError { get; set; }
    public int LastImportedCount { get; set; }
    public int LastUpdatedCount { get; set; }
    public int LastUnchangedCount { get; set; }
    public int LastWarningCount { get; set; }
    public int LastErrorCount { get; set; }
}
