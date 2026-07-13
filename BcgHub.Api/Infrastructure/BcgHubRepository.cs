using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BcgHub.Api.Infrastructure;

public sealed class BcgHubRepository(BcgHubDbContext db) : IOrderReadRepository, IOrderWriteRepository, IPohodaImportRepository
{
    public async Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, Guid? customerId, OrderSalesChannel salesChannel, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (salesChannel == OrderSalesChannel.Eshop) query = query.Where(x => x.PohodaOrderId != null && EF.Functions.ILike(x.PohodaOrderId, "%hop%"));
        if (salesChannel == OrderSalesChannel.Wholesale) query = query.Where(x => x.PohodaOrderId == null || !EF.Functions.ILike(x.PohodaOrderId, "%hop%"));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLike(search.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Number, pattern, "\\") || EF.Functions.ILike(x.Title, pattern, "\\") || EF.Functions.ILike(x.Customer.Name, pattern, "\\"));
        }

        query = (sortBy.ToLowerInvariant(), descending) switch
        {
            ("customer", false) => query.OrderBy(x => x.Customer.Name).ThenBy(x => x.Id),
            ("customer", true) => query.OrderByDescending(x => x.Customer.Name).ThenBy(x => x.Id),
            ("title", false) => query.OrderBy(x => x.Title).ThenBy(x => x.Id),
            ("title", true) => query.OrderByDescending(x => x.Title).ThenBy(x => x.Id),
            ("received", false) => query.OrderBy(x => x.OrderedOn).ThenBy(x => x.Id),
            ("received", true) => query.OrderByDescending(x => x.OrderedOn).ThenBy(x => x.Id),
            ("carrier", false) => query.OrderBy(x => x.Carrier != null ? x.Carrier.Name : null).ThenBy(x => x.Id),
            ("carrier", true) => query.OrderByDescending(x => x.Carrier != null ? x.Carrier.Name : null).ThenBy(x => x.Id),
            ("delivery", false) => query.OrderBy(x => x.PlannedDeliveryOn).ThenBy(x => x.Id),
            ("delivery", true) => query.OrderByDescending(x => x.PlannedDeliveryOn).ThenBy(x => x.Id),
            ("value", false) => query.OrderBy(x => x.ValueCzk).ThenBy(x => x.Id),
            ("value", true) => query.OrderByDescending(x => x.ValueCzk).ThenBy(x => x.Id),
            (_, false) => query.OrderBy(x => x.Number).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Number).ThenBy(x => x.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new OrderListItem(x.Id, x.Number, x.Title, x.Customer.Name, x.Status, x.PlannedDeliveryOn, x.ValueCzk, x.WeightKg, x.WorkflowSteps.Count(s => s.Status == WorkflowStepStatus.Completed || s.Status == WorkflowStepStatus.NotRequired), x.WorkflowSteps.Count, x.OrderedOn, x.Carrier != null ? x.Carrier.Name : null)).ToListAsync(cancellationToken);
        return new PagedResult<OrderListItem>(items, page, pageSize, totalCount);
    }

    public async Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var header = await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => new OrderDetailHeader(x.Id, x.Number, x.PohodaOrderNumber, x.Title, x.Status, new PartnerReference(x.Customer.Id, x.Customer.Name), x.CustomerContact != null ? x.CustomerContact.FullName : null, x.Warehouse != null ? new PartnerReference(x.Warehouse.Id, x.Warehouse.Name) : null, x.Carrier != null ? new PartnerReference(x.Carrier.Id, x.Carrier.Name) : null, x.CustomsDeclarant != null ? new PartnerReference(x.CustomsDeclarant.Id, x.CustomsDeclarant.Name) : null, x.OrderedOn, x.RequestedDeliveryOn, x.PlannedPickupOn, x.PlannedDeliveryOn, x.ValueCzk, x.WeightKg, x.VolumeM3, x.WarehouseInstructions, x.CustomerContactId, x.Version)).SingleOrDefaultAsync(cancellationToken);
        if (header is null) return null;

        var definitions = WorkflowCatalog.All.ToDictionary(x => x.Type);
        var stepRows = await db.OrderWorkflowSteps.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Type).Select(x => new { x.Id, x.Type, x.Status, x.DueAtUtc, x.CompletedAtUtc, x.Notes, x.Version }).ToListAsync(cancellationToken);
        var steps = stepRows.Select(x => new WorkflowStepDto(x.Id, x.Type, definitions[x.Type].Title, definitions[x.Type].Description, x.Status, x.DueAtUtc, x.CompletedAtUtc, x.Notes, x.Version)).ToList();
        var quotes = await db.TransportQuotes.AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.IsSelected).ThenBy(x => x.Price).Select(x => new TransportQuoteDto(x.Id, new PartnerReference(x.Carrier.Id, x.Carrier.Name), x.Price, x.Currency, x.PickupOn, x.DeliveryOn, x.IsSelected, x.Notes, x.Version)).ToListAsync(cancellationToken);
        return new OrderDetailDto(header.Id, header.Number, header.PohodaOrderNumber, header.Title, header.Status, header.Customer, header.CustomerContact, header.Warehouse, header.Carrier, header.CustomsDeclarant, header.OrderedOn, header.RequestedDeliveryOn, header.PlannedPickupOn, header.PlannedDeliveryOn, header.ValueCzk, header.WeightKg, header.VolumeM3, header.WarehouseInstructions, steps, quotes, header.CustomerContactId, header.Version);
    }

    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken) => db.Orders.AnyAsync(x => x.Number == number, cancellationToken);

    public async Task<OrderReferenceValidation> ValidateReferencesAsync(Guid customerId, Guid? contactId, Guid? warehouseId, Guid? carrierId, Guid? customsDeclarantId, CancellationToken cancellationToken)
    {
        var partnerIds = new Guid?[] { customerId, warehouseId, carrierId, customsDeclarantId }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var partnerTypes = await db.BusinessPartners.AsNoTracking().Where(x => partnerIds.Contains(x.Id)).Select(x => new { x.Id, x.Type }).ToDictionaryAsync(x => x.Id, x => x.Type, cancellationToken);
        var customerIsValid = partnerTypes.GetValueOrDefault(customerId) == PartnerType.Customer;
        var contactMatches = contactId is null || await db.ContactPeople.AnyAsync(x => x.Id == contactId && x.BusinessPartnerId == customerId, cancellationToken);
        var warehouseIsValid = warehouseId is null || partnerTypes.GetValueOrDefault(warehouseId.Value) == PartnerType.Warehouse;
        var carrierIsValid = carrierId is null || partnerTypes.GetValueOrDefault(carrierId.Value) == PartnerType.Carrier;
        var customsDeclarantIsValid = customsDeclarantId is null || partnerTypes.GetValueOrDefault(customsDeclarantId.Value) == PartnerType.CustomsDeclarant;
        return new OrderReferenceValidation(customerIsValid, contactMatches, warehouseIsValid, carrierIsValid, customsDeclarantIsValid);
    }

    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<OrderWorkflowStep?> GetWorkflowStepAsync(Guid orderId, Guid stepId, CancellationToken cancellationToken) => db.OrderWorkflowSteps.SingleOrDefaultAsync(x => x.OrderId == orderId && x.Id == stepId, cancellationToken);
    public Task<Order?> GetOrderAsync(Guid id, CancellationToken cancellationToken) => db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public void SetOriginalVersion(Order order, uint version) => db.Entry(order).Property(x => x.Version).OriginalValue = version;
    public void Remove(Order order) => db.Orders.Remove(order);
    public Task<TransportQuote?> GetQuoteAsync(Guid orderId, Guid quoteId, CancellationToken cancellationToken) => db.TransportQuotes.Include(x => x.Carrier).SingleOrDefaultAsync(x => x.OrderId == orderId && x.Id == quoteId, cancellationToken);
    public void SetOriginalVersion(TransportQuote quote, uint version) => db.Entry(quote).Property(x => x.Version).OriginalValue = version;
    public Task<bool> IsCarrierAsync(Guid carrierId, CancellationToken cancellationToken) => db.BusinessPartners.AnyAsync(x => x.Id == carrierId && x.Type == PartnerType.Carrier, cancellationToken);
    public async Task ClearSelectedQuoteAsync(Guid orderId, Guid? exceptId, CancellationToken cancellationToken) => await db.TransportQuotes.Where(x => x.OrderId == orderId && x.IsSelected && x.Id != exceptId).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsSelected, false).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
    public void AddQuote(TransportQuote quote) => db.TransportQuotes.Add(quote);
    public void RemoveQuote(TransportQuote quote) => db.TransportQuotes.Remove(quote);
    public void SetOriginalVersion(OrderWorkflowStep step, uint version) => db.Entry(step).Property(x => x.Version).OriginalValue = version;
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("Záznam mezitím změnil jiný uživatel. Načtěte aktuální data a zkuste to znovu."); }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { throw new DomainValidationException("Záznam se stejným unikátním údajem již existuje."); }
    }

    public async Task<IReadOnlyDictionary<string, Guid>> FindCustomersAsync(IEnumerable<PohodaCustomerData> customers, CancellationToken cancellationToken)
    {
        var requestedKeys = customers.Select(PohodaOrderImportService.CustomerKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existing = await db.BusinessPartners.AsNoTracking().Where(x => x.Type == PartnerType.Customer).Select(x => new { x.Id, x.Name, x.CompanyNumber, x.VatNumber, x.CountryCode }).ToListAsync(cancellationToken);
        return existing.Select(x => new { x.Id, Key = PohodaOrderImportService.CustomerKey(new PohodaCustomerData(x.Name, x.CompanyNumber, x.VatNumber, null, null, null, null, null, x.CountryCode)) }).Where(x => requestedKeys.Contains(x.Key)).GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, Order>> FindExistingPohodaOrdersAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken) => (await db.Orders.Include(x => x.Customer).Where(x => x.PohodaOrderId != null && externalIds.Contains(x.PohodaOrderId)).ToListAsync(cancellationToken)).ToDictionary(x => x.PohodaOrderId!, StringComparer.OrdinalIgnoreCase);
    public async Task<int> GetNextOrderSequenceAsync(int year, CancellationToken cancellationToken) { var prefix = $"BCG_{year}"; var number = await db.Orders.AsNoTracking().Where(x => x.Number.StartsWith(prefix) && x.Number.Length == prefix.Length + 4).OrderByDescending(x => x.Number).Select(x => x.Number).FirstOrDefaultAsync(cancellationToken); return number is not null && int.TryParse(number[prefix.Length..], out var sequence) ? sequence + 1 : 1; }
    public void AddImportedCustomer(BusinessPartner customer) => db.BusinessPartners.Add(customer);
    public void AddImportedOrder(Order order) => db.Orders.Add(order);
    public Task SaveImportAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    private sealed record OrderDetailHeader(Guid Id, string Number, string? PohodaOrderNumber, string Title, OrderStatus Status, PartnerReference Customer, string? CustomerContact, PartnerReference? Warehouse, PartnerReference? Carrier, PartnerReference? CustomsDeclarant, DateOnly? OrderedOn, DateOnly? RequestedDeliveryOn, DateOnly? PlannedPickupOn, DateOnly? PlannedDeliveryOn, decimal ValueCzk, decimal WeightKg, decimal VolumeM3, string? WarehouseInstructions, Guid? CustomerContactId, uint Version);
}
