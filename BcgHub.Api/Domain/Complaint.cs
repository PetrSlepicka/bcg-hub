namespace BcgHub.Api.Domain;

public enum ComplaintStatus { New, InProgress, Resolved, Rejected }

public sealed class Complaint : Entity
{
    public DateOnly ReportedOn { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;
    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string? Description { get; set; }
}
