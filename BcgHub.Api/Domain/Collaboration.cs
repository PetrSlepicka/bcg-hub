namespace BcgHub.Api.Domain;

public interface IEntityResource
{
    Guid? BusinessPartnerId { get; set; }
    BusinessPartner? BusinessPartner { get; set; }
    Guid? ContactPersonId { get; set; }
    ContactPerson? ContactPerson { get; set; }
    Guid? OrderId { get; set; }
    Order? Order { get; set; }
    Guid? WorkflowStepId { get; set; }
    OrderWorkflowStep? WorkflowStep { get; set; }
    Guid? TransportQuoteId { get; set; }
    TransportQuote? TransportQuote { get; set; }
    Guid? CommunicationId { get; set; }
    Communication? Communication { get; set; }
    Guid? EmailMessageId { get; set; }
    EmailMessage? EmailMessage { get; set; }
    Guid? ComplaintId { get; set; }
    Complaint? Complaint { get; set; }
}

public sealed class Comment : Entity, IEntityResource
{
    public Guid? BusinessPartnerId { get; set; }
    public BusinessPartner? BusinessPartner { get; set; }
    public Guid? ContactPersonId { get; set; }
    public ContactPerson? ContactPerson { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? WorkflowStepId { get; set; }
    public OrderWorkflowStep? WorkflowStep { get; set; }
    public Guid? TransportQuoteId { get; set; }
    public TransportQuote? TransportQuote { get; set; }
    public Guid? CommunicationId { get; set; }
    public Communication? Communication { get; set; }
    public Guid? EmailMessageId { get; set; }
    public EmailMessage? EmailMessage { get; set; }
    public Guid? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public string AuthorName { get; set; } = "";
    public string Text { get; set; } = "";
}

public sealed class Attachment : Entity, IEntityResource
{
    public Guid? BusinessPartnerId { get; set; }
    public BusinessPartner? BusinessPartner { get; set; }
    public Guid? ContactPersonId { get; set; }
    public ContactPerson? ContactPerson { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? WorkflowStepId { get; set; }
    public OrderWorkflowStep? WorkflowStep { get; set; }
    public Guid? TransportQuoteId { get; set; }
    public TransportQuote? TransportQuote { get; set; }
    public Guid? CommunicationId { get; set; }
    public Communication? Communication { get; set; }
    public Guid? EmailMessageId { get; set; }
    public EmailMessage? EmailMessage { get; set; }
    public Guid? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
    public long Size { get; set; }
    public string StorageKey { get; set; } = "";
}

public enum CommunicationType
{
    Email,
    PhoneCall,
    Meeting,
    Note
}

public sealed class Communication : Entity
{
    public CommunicationType Type { get; set; }
    public Guid? BusinessPartnerId { get; set; }
    public BusinessPartner? BusinessPartner { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public string Subject { get; set; } = "";
    public string? BodyPreview { get; set; }
    public string? Sender { get; set; }
    public string? Recipients { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? ExternalProvider { get; set; }
    public string? ExternalMailboxId { get; set; }
    public string? ExternalId { get; set; }
}
