using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BcgHub.Api.Infrastructure;

public sealed class BcgHubRepository(BcgHubDbContext db) : IOrderReadRepository, IOrderWriteRepository, IPohodaImportRepository
{
    public async Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, Guid? customerId, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
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
        var order = await db.Orders.AsNoTracking().Include(x => x.Customer).Include(x => x.CustomerContact).Include(x => x.Warehouse).Include(x => x.Carrier).Include(x => x.CustomsDeclarant).Include(x => x.WorkflowSteps).Include(x => x.TransportQuotes).ThenInclude(x => x.Carrier).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return order is null ? null : MapDetail(order);
    }

    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken) => db.Orders.AnyAsync(x => x.Number == number, cancellationToken);

    public async Task<OrderReferenceValidation> ValidateReferencesAsync(Guid customerId, Guid? contactId, Guid? warehouseId, Guid? carrierId, Guid? customsDeclarantId, CancellationToken cancellationToken)
    {
        var customerIsValid = await db.BusinessPartners.AnyAsync(x => x.Id == customerId && x.Type == PartnerType.Customer, cancellationToken);
        var contactMatches = contactId is null || await db.ContactPeople.AnyAsync(x => x.Id == contactId && x.BusinessPartnerId == customerId, cancellationToken);
        var warehouseIsValid = warehouseId is null || await db.BusinessPartners.AnyAsync(x => x.Id == warehouseId && x.Type == PartnerType.Warehouse, cancellationToken);
        var carrierIsValid = carrierId is null || await db.BusinessPartners.AnyAsync(x => x.Id == carrierId && x.Type == PartnerType.Carrier, cancellationToken);
        var customsDeclarantIsValid = customsDeclarantId is null || await db.BusinessPartners.AnyAsync(x => x.Id == customsDeclarantId && x.Type == PartnerType.CustomsDeclarant, cancellationToken);
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

    public async Task<IReadOnlySet<string>> FindExistingPohodaOrderIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken) => (await db.Orders.AsNoTracking().Where(x => x.PohodaOrderId != null && externalIds.Contains(x.PohodaOrderId)).Select(x => x.PohodaOrderId!).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    public async Task<int> GetNextOrderSequenceAsync(int year, CancellationToken cancellationToken) { var prefix = $"BCG_{year}"; var numbers = await db.Orders.AsNoTracking().Where(x => x.Number.StartsWith(prefix)).Select(x => x.Number).ToListAsync(cancellationToken); return numbers.Select(x => int.TryParse(x[prefix.Length..], out var sequence) ? sequence : 0).DefaultIfEmpty().Max() + 1; }
    public void AddImportedCustomer(BusinessPartner customer) => db.BusinessPartners.Add(customer);
    public void AddImportedOrder(Order order) => db.Orders.Add(order);
    public Task SaveImportAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    private static OrderDetailDto MapDetail(Order order)
    {
        var definitions = WorkflowCatalog.All.ToDictionary(x => x.Type);
        var steps = order.WorkflowSteps.OrderBy(x => x.Type).Select(x => new WorkflowStepDto(x.Id, x.Type, definitions[x.Type].Title, definitions[x.Type].Description, x.Status, x.DueAtUtc, x.CompletedAtUtc, x.Notes, x.Version)).ToList();
        var quotes = order.TransportQuotes.OrderByDescending(x => x.IsSelected).ThenBy(x => x.Price).Select(x => new TransportQuoteDto(x.Id, new PartnerReference(x.Carrier.Id, x.Carrier.Name), x.Price, x.Currency, x.PickupOn, x.DeliveryOn, x.IsSelected, x.Notes, x.Version)).ToList();
        return new OrderDetailDto(order.Id, order.Number, order.PohodaOrderNumber, order.Title, order.Status, new PartnerReference(order.Customer.Id, order.Customer.Name), order.CustomerContact?.FullName, ToReference(order.Warehouse), ToReference(order.Carrier), ToReference(order.CustomsDeclarant), order.OrderedOn, order.RequestedDeliveryOn, order.PlannedPickupOn, order.PlannedDeliveryOn, order.ValueCzk, order.WeightKg, order.VolumeM3, order.WarehouseInstructions, steps, quotes, order.CustomerContactId, order.Version);
    }

    private static PartnerReference? ToReference(BusinessPartner? partner) => partner is null ? null : new PartnerReference(partner.Id, partner.Name);
    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
