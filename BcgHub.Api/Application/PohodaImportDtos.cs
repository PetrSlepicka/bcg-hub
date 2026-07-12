namespace BcgHub.Api.Application;

public enum PohodaImportRowStatus { New, Duplicate, Error }
public sealed record PohodaImportRow(string ExternalId, string? PohodaOrderNumber, string Title, string CustomerName, string? CompanyNumber, DateOnly? OrderedOn, DateOnly? RequestedDeliveryOn, decimal ValueCzk, PohodaImportRowStatus Status, string? Message);
public sealed record PohodaImportPreview(IReadOnlyList<PohodaImportRow> Rows, int NewCount, int DuplicateCount, int ErrorCount);
public sealed record PohodaImportResult(int ImportedCount, int DuplicateCount, int ErrorCount);
