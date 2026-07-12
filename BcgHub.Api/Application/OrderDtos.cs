using System.ComponentModel.DataAnnotations;
using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed record OrderListItem(Guid Id, string Number, string Title, string CustomerName, OrderStatus Status, DateOnly? PlannedDeliveryOn, decimal ValueCzk, decimal WeightKg, int CompletedSteps, int TotalSteps, DateOnly? OrderedOn, string? CarrierName);
public sealed record PartnerReference(Guid Id, string Name);
public sealed record WorkflowStepDto(Guid Id, WorkflowStepType Type, string Title, string Description, WorkflowStepStatus Status, DateTime? DueAtUtc, DateTime? CompletedAtUtc, string? Notes, uint Version);
public sealed record TransportQuoteDto(Guid Id, PartnerReference Carrier, decimal Price, string Currency, DateOnly? PickupOn, DateOnly? DeliveryOn, bool IsSelected, string? Notes, uint Version);
public sealed record OrderDetailDto(Guid Id, string Number, string? PohodaOrderNumber, string Title, OrderStatus Status, PartnerReference Customer, string? CustomerContact, PartnerReference? Warehouse, PartnerReference? Carrier, PartnerReference? CustomsDeclarant, DateOnly? OrderedOn, DateOnly? RequestedDeliveryOn, DateOnly? PlannedPickupOn, DateOnly? PlannedDeliveryOn, decimal ValueCzk, decimal WeightKg, decimal VolumeM3, string? WarehouseInstructions, IReadOnlyList<WorkflowStepDto> WorkflowSteps, IReadOnlyList<TransportQuoteDto> TransportQuotes, Guid? CustomerContactId, uint Version);
public class CreateOrderRequest
{
    [Required, StringLength(50)] public string Number { get; init; } = "";
    [StringLength(50)] public string? PohodaOrderNumber { get; init; }
    [Required, StringLength(300)] public string Title { get; init; } = "";
    public Guid CustomerId { get; init; }
    public Guid? CustomerContactId { get; init; }
    public Guid? WarehouseId { get; init; }
    public Guid? CarrierId { get; init; }
    public Guid? CustomsDeclarantId { get; init; }
    [EnumDataType(typeof(OrderStatus))] public OrderStatus Status { get; init; } = OrderStatus.New;
    public DateOnly? OrderedOn { get; init; }
    public DateOnly? RequestedDeliveryOn { get; init; }
    public DateOnly? PlannedPickupOn { get; init; }
    public DateOnly? PlannedDeliveryOn { get; init; }
    [Range(0, 9999999999999999d)] public decimal ValueCzk { get; init; }
    [Range(0, 999999999999999d)] public decimal WeightKg { get; init; }
    [Range(0, 999999999999999d)] public decimal VolumeM3 { get; init; }
    [StringLength(10000)] public string? WarehouseInstructions { get; init; }
}

public sealed class UpdateOrderRequest : CreateOrderRequest { public uint Version { get; init; } }

public sealed class SaveTransportQuoteRequest
{
    public Guid CarrierId { get; init; }
    [Range(0, 9999999999999999d)] public decimal Price { get; init; }
    [Required, StringLength(3, MinimumLength = 3)] public string Currency { get; init; } = "EUR";
    public DateOnly? PickupOn { get; init; }
    public DateOnly? DeliveryOn { get; init; }
    public bool IsSelected { get; init; }
    [StringLength(5000)] public string? Notes { get; init; }
    public uint Version { get; init; }
}

public sealed class UpdateWorkflowStepRequest
{
    [EnumDataType(typeof(WorkflowStepStatus))] public WorkflowStepStatus Status { get; init; }
    [StringLength(5000)] public string? Notes { get; init; }
    public DateTime? DueAtUtc { get; init; }
    public uint Version { get; init; }
}

public sealed record PartnerListItem(Guid Id, PartnerType Type, string Name, string? City, string? CountryCode, string? Email, string? Phone, int ContactCount);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
