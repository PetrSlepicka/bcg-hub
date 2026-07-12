using System.ComponentModel.DataAnnotations;
using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed record ComplaintListItem(Guid Id, DateOnly ReportedOn, ComplaintStatus Status, PartnerReference Customer, Guid OrderId, string OrderNumber, string? Description);
public sealed record ComplaintDetailDto(Guid Id, DateOnly ReportedOn, ComplaintStatus Status, PartnerReference Customer, Guid OrderId, string OrderNumber, string? Description, uint Version);
public sealed class SaveComplaintRequest
{
    public DateOnly ReportedOn { get; init; }
    [EnumDataType(typeof(ComplaintStatus))] public ComplaintStatus Status { get; init; }
    public Guid CustomerId { get; init; }
    public Guid OrderId { get; init; }
    [StringLength(10000)] public string? Description { get; init; }
    public uint Version { get; init; }
}
