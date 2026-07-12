using System.ComponentModel.DataAnnotations;
using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public enum ResourceOwnerType { BusinessPartner, ContactPerson, Order, WorkflowStep, TransportQuote, Communication, EmailMessage, Complaint }
public sealed record CommentDto(Guid Id, string AuthorName, string Text, DateTime CreatedAtUtc, uint Version);
public sealed record AttachmentDto(Guid Id, string FileName, string ContentType, long Size, DateTime CreatedAtUtc, uint Version);
public sealed class SaveCommentRequest { [Required, StringLength(10000)] public string Text { get; init; } = ""; public uint Version { get; init; } }
public sealed record StoredFile(Stream Stream, string FileName, string ContentType);
public sealed record CommunicationDto(Guid Id, CommunicationType Type, Guid? BusinessPartnerId, Guid? OrderId, string Subject, string? BodyPreview, string? Sender, string? Recipients, DateTime OccurredAtUtc, uint Version);
public sealed class SaveCommunicationRequest
{
    [EnumDataType(typeof(CommunicationType))] public CommunicationType Type { get; init; }
    public Guid? BusinessPartnerId { get; init; }
    public Guid? OrderId { get; init; }
    [Required, StringLength(1000)] public string Subject { get; init; } = "";
    [StringLength(10000)] public string? BodyPreview { get; init; }
    [StringLength(1000)] public string? Sender { get; init; }
    [StringLength(4000)] public string? Recipients { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public uint Version { get; init; }
}
