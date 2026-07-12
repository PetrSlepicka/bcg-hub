using BcgHub.Api.Domain;
using System.Text.RegularExpressions;

namespace BcgHub.Api.Application;

public sealed class OrderQueryService(IOrderReadRepository repository) : IOrderQueryService
{
    public Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, Guid? customerId, CancellationToken cancellationToken) => repository.GetListAsync(search, sortBy, descending, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), customerId, cancellationToken);
    public Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken) => repository.GetDetailAsync(id, cancellationToken);
}

public sealed class OrderCommandService(IOrderWriteRepository repository, IOrderReadRepository readRepository) : IOrderCommandService
{
    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var number = request.Number.Trim();
        var title = request.Title.Trim();
        if (number.Length == 0) throw new DomainValidationException("Číslo zakázky je povinné.");
        ValidateNumber(number);
        if (title.Length == 0) throw new DomainValidationException("Název zakázky je povinný.");
        if (request.CustomerId == Guid.Empty) throw new DomainValidationException("Zákazník je povinný.");
        if (await repository.NumberExistsAsync(number, cancellationToken)) throw new DomainValidationException("Zakázka se stejným číslem již existuje.");

        await ValidateReferencesAsync(request, cancellationToken);

        var order = new Order();
        Apply(order, request);
        order.WorkflowSteps = WorkflowCatalog.CreateSteps(order.Id);
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return (await readRepository.GetDetailAsync(order.Id, cancellationToken))!;
    }

    public async Task<OrderDetailDto?> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await repository.GetOrderAsync(id, cancellationToken);
        if (order is null) return null;
        ValidateNumber(request.Number.Trim());
        if (!string.Equals(order.Number, request.Number.Trim(), StringComparison.OrdinalIgnoreCase) && await repository.NumberExistsAsync(request.Number.Trim(), cancellationToken)) throw new DomainValidationException("Zakázka se stejným číslem již existuje.");
        await ValidateReferencesAsync(request, cancellationToken);
        repository.SetOriginalVersion(order, request.Version);
        Apply(order, request);
        await repository.SaveChangesAsync(cancellationToken);
        return await readRepository.GetDetailAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken)
    {
        var order = await repository.GetOrderAsync(id, cancellationToken);
        if (order is null) return false;
        repository.SetOriginalVersion(order, version);
        repository.Remove(order);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WorkflowStepDto?> UpdateStepAsync(Guid orderId, Guid stepId, UpdateWorkflowStepRequest request, CancellationToken cancellationToken)
    {
        var step = await repository.GetWorkflowStepAsync(orderId, stepId, cancellationToken);
        if (step is null) return null;
        repository.SetOriginalVersion(step, request.Version);
        step.Status = request.Status;
        step.Notes = request.Notes?.Trim();
        step.DueAtUtc = request.DueAtUtc;
        step.CompletedAtUtc = request.Status == WorkflowStepStatus.Completed ? DateTime.UtcNow : null;
        step.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        var definition = WorkflowCatalog.All.Single(x => x.Type == step.Type);
        return new WorkflowStepDto(step.Id, step.Type, definition.Title, definition.Description, step.Status, step.DueAtUtc, step.CompletedAtUtc, step.Notes, step.Version);
    }

    public async Task<TransportQuoteDto?> AddQuoteAsync(Guid orderId, SaveTransportQuoteRequest request, CancellationToken cancellationToken)
    {
        if (await repository.GetOrderAsync(orderId, cancellationToken) is null) return null;
        await ValidateCarrierAsync(request.CarrierId, cancellationToken);
        if (request.IsSelected) await repository.ClearSelectedQuoteAsync(orderId, null, cancellationToken);
        var quote = new TransportQuote { OrderId = orderId };
        Apply(quote, request);
        repository.AddQuote(quote);
        await repository.SaveChangesAsync(cancellationToken);
        return await MapQuoteAsync(orderId, quote.Id, cancellationToken);
    }

    public async Task<TransportQuoteDto?> UpdateQuoteAsync(Guid orderId, Guid quoteId, SaveTransportQuoteRequest request, CancellationToken cancellationToken)
    {
        var quote = await repository.GetQuoteAsync(orderId, quoteId, cancellationToken);
        if (quote is null) return null;
        await ValidateCarrierAsync(request.CarrierId, cancellationToken);
        repository.SetOriginalVersion(quote, request.Version);
        if (request.IsSelected) await repository.ClearSelectedQuoteAsync(orderId, quoteId, cancellationToken);
        Apply(quote, request);
        await repository.SaveChangesAsync(cancellationToken);
        return await MapQuoteAsync(orderId, quoteId, cancellationToken);
    }

    public async Task<bool> DeleteQuoteAsync(Guid orderId, Guid quoteId, uint version, CancellationToken cancellationToken)
    {
        var quote = await repository.GetQuoteAsync(orderId, quoteId, cancellationToken);
        if (quote is null) return false;
        repository.SetOriginalVersion(quote, version);
        repository.RemoveQuote(quote);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var references = await repository.ValidateReferencesAsync(request.CustomerId, request.CustomerContactId, request.WarehouseId, request.CarrierId, request.CustomsDeclarantId, cancellationToken);
        if (!references.CustomerIsValid) throw new DomainValidationException("Vybraný partner není zákazník.");
        if (!references.ContactMatchesCustomer) throw new DomainValidationException("Kontaktní osoba nepatří vybranému zákazníkovi.");
        if (!references.WarehouseIsValid) throw new DomainValidationException("Vybraný partner není sklad.");
        if (!references.CarrierIsValid) throw new DomainValidationException("Vybraný partner není dopravce.");
        if (!references.CustomsDeclarantIsValid) throw new DomainValidationException("Vybraný partner není celní deklarant.");
    }

    private async Task ValidateCarrierAsync(Guid carrierId, CancellationToken cancellationToken) { if (!await repository.IsCarrierAsync(carrierId, cancellationToken)) throw new DomainValidationException("Vybraný partner není dopravce."); }
    private async Task<TransportQuoteDto?> MapQuoteAsync(Guid orderId, Guid quoteId, CancellationToken cancellationToken) => (await readRepository.GetDetailAsync(orderId, cancellationToken))?.TransportQuotes.Single(x => x.Id == quoteId);
    private static void Apply(Order order, CreateOrderRequest request) { order.Number = Required(request.Number, "Číslo zakázky").ToUpperInvariant(); order.PohodaOrderNumber = Clean(request.PohodaOrderNumber); order.Title = Required(request.Title, "Název zakázky"); order.Status = request.Status; order.CustomerId = request.CustomerId; order.CustomerContactId = request.CustomerContactId; order.WarehouseId = request.WarehouseId; order.CarrierId = request.CarrierId; order.CustomsDeclarantId = request.CustomsDeclarantId; order.OrderedOn = request.OrderedOn; order.RequestedDeliveryOn = request.RequestedDeliveryOn; order.PlannedPickupOn = request.PlannedPickupOn; order.PlannedDeliveryOn = request.PlannedDeliveryOn; order.ValueCzk = request.ValueCzk; order.WeightKg = request.WeightKg; order.VolumeM3 = request.VolumeM3; order.WarehouseInstructions = Clean(request.WarehouseInstructions); }
    private static void Apply(TransportQuote quote, SaveTransportQuoteRequest request) { quote.CarrierId = request.CarrierId; quote.Price = request.Price; quote.Currency = Required(request.Currency, "Měna").ToUpperInvariant(); quote.PickupOn = request.PickupOn; quote.DeliveryOn = request.DeliveryOn; quote.IsSelected = request.IsSelected; quote.Notes = Clean(request.Notes); }
    private static string Required(string value, string label) => string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException($"{label} je povinné.") : value.Trim();
    private static void ValidateNumber(string number) { if (!Regex.IsMatch(number, @"^BCG_\d{8}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) throw new DomainValidationException("Číslo zakázky musí být ve formátu BCG_RRRRNNNN."); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
