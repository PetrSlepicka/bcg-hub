namespace BcgHub.Api.Domain;

public enum OrderStatus
{
    New,
    InProgress,
    Waiting,
    ReadyForPickup,
    InTransit,
    Completed,
    Cancelled
}

public sealed class Order : Entity
{
    public string Number { get; set; } = "";
    public string? PohodaOrderNumber { get; set; }
    public string? PohodaOrderId { get; set; }
    public string Title { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public Guid CustomerId { get; set; }
    public BusinessPartner Customer { get; set; } = null!;
    public Guid? CustomerContactId { get; set; }
    public ContactPerson? CustomerContact { get; set; }
    public Guid? WarehouseId { get; set; }
    public BusinessPartner? Warehouse { get; set; }
    public Guid? CarrierId { get; set; }
    public BusinessPartner? Carrier { get; set; }
    public Guid? CustomsDeclarantId { get; set; }
    public BusinessPartner? CustomsDeclarant { get; set; }
    public DateOnly? OrderedOn { get; set; }
    public DateOnly? RequestedDeliveryOn { get; set; }
    public DateOnly? PlannedPickupOn { get; set; }
    public DateOnly? PlannedDeliveryOn { get; set; }
    public decimal ValueCzk { get; set; }
    public decimal WeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
    public string? WarehouseInstructions { get; set; }
    public ICollection<OrderWorkflowStep> WorkflowSteps { get; set; } = [];
    public ICollection<TransportQuote> TransportQuotes { get; set; } = [];
}

public enum WorkflowStepType
{
    OrderCreatedInPohoda = 1,
    InstructionsPrepared = 2,
    OrderIssuedFromPohoda = 3,
    SentToWarehouse = 4,
    WarehouseReady = 5,
    TransportQuotesReceived = 6,
    TransportSelected = 7,
    InvoiceGenerated = 8,
    GoodsIssuedFromPohoda = 9,
    PickupAnnouncedToWarehouse = 10,
    ExportDocumentsPrepared = 11,
    DocumentsSentToWarehouse = 12,
    CustomerInformed = 13,
    PickupConfirmed = 14,
    ConfirmedExportDocumentsReceived = 15
}

public enum WorkflowStepStatus
{
    Pending,
    InProgress,
    Waiting,
    Completed,
    NotRequired
}

public sealed class OrderWorkflowStep : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public WorkflowStepType Type { get; set; }
    public WorkflowStepStatus Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class TransportQuote : Entity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid CarrierId { get; set; }
    public BusinessPartner Carrier { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateOnly? PickupOn { get; set; }
    public DateOnly? DeliveryOn { get; set; }
    public bool IsSelected { get; set; }
    public string? Notes { get; set; }
}
