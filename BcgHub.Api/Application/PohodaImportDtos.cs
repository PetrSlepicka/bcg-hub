namespace BcgHub.Api.Application;

public enum PohodaImportRowStatus { New, Updated, Unchanged, Warning, Error }
public sealed record PohodaImportRow(string ExternalId, string? PohodaOrderNumber, string Title, string CustomerName, string? CompanyNumber, DateOnly? OrderedOn, DateOnly? RequestedDeliveryOn, decimal ValueCzk, PohodaImportRowStatus Status, string? Message);
public sealed record PohodaImportPreview(IReadOnlyList<PohodaImportRow> Rows, int NewCount, int UpdatedCount, int UnchangedCount, int WarningCount, int ErrorCount);
public sealed record PohodaImportResult(int ImportedCount, int UpdatedCount, int UnchangedCount, int WarningCount, int ErrorCount);
public sealed record PohodaSyncResult(string RunId, string Trigger, DateTime RequestedChangesSinceUtc, DateTime StartedAtUtc, DateTime CompletedAtUtc, int ImportedCount, int UpdatedCount, int UnchangedCount, int WarningCount, int ErrorCount);
public sealed record PohodaSyncStatus(bool Enabled, string Endpoint, int IntervalMinutes, DateTime? LastAttemptStartedAtUtc, DateTime? LastAttemptCompletedAtUtc, DateTime? LastSuccessfulSyncUtc, string? LastRunId, string? LastTrigger, string? LastError, int LastImportedCount, int LastUpdatedCount, int LastUnchangedCount, int LastWarningCount, int LastErrorCount);
