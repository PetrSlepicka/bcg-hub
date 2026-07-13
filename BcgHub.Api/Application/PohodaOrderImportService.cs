using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed class PohodaOrderImportService(IPohodaOrderXmlParser parser, IPohodaImportRepository repository, ILogger<PohodaOrderImportService> logger) : IPohodaOrderImportService
{
    public async Task<PohodaImportPreview> PreviewAsync(Stream xml, CancellationToken cancellationToken)
    {
        var data = parser.Parse(xml);
        var customers = await repository.FindCustomersAsync(data.Select(x => x.Customer), cancellationToken);
        var existing = await repository.FindExistingPohodaOrdersAsync(data.Select(x => x.ExternalId), cancellationToken);
        var duplicateIds = DuplicateIds(data);
        var rows = data.Select(order => CreateRow(order, customers, existing, duplicateIds)).ToList();
        return new PohodaImportPreview(rows, Count(rows, PohodaImportRowStatus.New), Count(rows, PohodaImportRowStatus.Updated), Count(rows, PohodaImportRowStatus.Unchanged), Count(rows, PohodaImportRowStatus.Warning), Count(rows, PohodaImportRowStatus.Error));
    }

    public Task<PohodaImportResult> ImportAsync(Stream xml, CancellationToken cancellationToken) => ImportAsync(parser.Parse(xml), cancellationToken);
    public Task<PohodaImportResult> ImportMServerResponseAsync(Stream xml, string companyNumber, CancellationToken cancellationToken) => ImportAsync(parser.Parse(xml, companyNumber, true), cancellationToken);

    private async Task<PohodaImportResult> ImportAsync(IReadOnlyList<PohodaOrderData> data, CancellationToken cancellationToken)
    {
        var customers = (await repository.FindCustomersAsync(data.Select(x => x.Customer), cancellationToken)).ToDictionary(StringComparer.OrdinalIgnoreCase);
        var existing = await repository.FindExistingPohodaOrdersAsync(data.Select(x => x.ExternalId), cancellationToken);
        var duplicateIds = DuplicateIds(data);
        var rows = data.Select(order => CreateRow(order, customers, existing, duplicateIds)).ToList();
        foreach (var row in rows.Where(x => x.Status is PohodaImportRowStatus.Warning or PohodaImportRowStatus.Error)) logger.LogWarning("POHODA import row {PohodaOrderId} has status {Status}: {Message}", row.ExternalId, row.Status, row.Message);
        var newOrders = data.Zip(rows).Where(x => x.Second.Status == PohodaImportRowStatus.New).Select(x => x.First).ToList();
        AddMissingCustomers(newOrders, customers);
        var sequences = new Dictionary<int, int>();
        var pending = 0;
        foreach (var (source, row) in data.Zip(rows))
        {
            if (row.Status == PohodaImportRowStatus.New) await AddOrderAsync(source, customers, sequences, cancellationToken);
            else if (row.Status is PohodaImportRowStatus.Updated or PohodaImportRowStatus.Warning && existing.TryGetValue(source.ExternalId, out var order) && HasPohodaChanges(order, source)) ApplyPohodaChanges(order, source);
            else continue;
            pending++;
            if (pending < 100) continue;
            await repository.SaveImportAsync(cancellationToken);
            logger.LogInformation("POHODA import persisted a batch of {BatchSize} changed orders.", pending);
            pending = 0;
        }
        if (pending > 0) { await repository.SaveImportAsync(cancellationToken); logger.LogInformation("POHODA import persisted the final batch of {BatchSize} changed orders.", pending); }
        var result = new PohodaImportResult(Count(rows, PohodaImportRowStatus.New), Count(rows, PohodaImportRowStatus.Updated), Count(rows, PohodaImportRowStatus.Unchanged), Count(rows, PohodaImportRowStatus.Warning), Count(rows, PohodaImportRowStatus.Error));
        logger.LogInformation("POHODA import completed. Imported: {ImportedCount}, updated: {UpdatedCount}, unchanged: {UnchangedCount}, warnings: {WarningCount}, errors: {ErrorCount}.", result.ImportedCount, result.UpdatedCount, result.UnchangedCount, result.WarningCount, result.ErrorCount);
        return result;
    }

    private async Task AddOrderAsync(PohodaOrderData source, IReadOnlyDictionary<string, Guid> customers, IDictionary<int, int> sequences, CancellationToken cancellationToken)
    {
        var year = source.OrderedOn?.Year ?? DateTime.UtcNow.Year;
        if (!sequences.TryGetValue(year, out var sequence)) sequence = await repository.GetNextOrderSequenceAsync(year, cancellationToken);
        sequences[year] = sequence + 1;
        var order = new Order { Number = $"BCG_{year}{sequence:0000}", PohodaOrderId = source.ExternalId, CustomerId = customers[CustomerKey(source.Customer)] };
        ApplyPohodaChanges(order, source);
        order.WorkflowSteps = WorkflowCatalog.CreateSteps(order.Id);
        var created = order.WorkflowSteps.Single(x => x.Type == WorkflowStepType.OrderCreatedInPohoda); created.Status = WorkflowStepStatus.Completed; created.CompletedAtUtc = DateTime.UtcNow;
        repository.AddImportedOrder(order);
    }

    private void AddMissingCustomers(IEnumerable<PohodaOrderData> orders, IDictionary<string, Guid> customers)
    {
        foreach (var source in orders.GroupBy(x => CustomerKey(x.Customer), StringComparer.OrdinalIgnoreCase).Select(x => x.First()))
        {
            var key = CustomerKey(source.Customer);
            if (customers.ContainsKey(key)) continue;
            var data = source.Customer;
            var customer = new BusinessPartner { Type = PartnerType.Customer, Name = data.Name, CompanyNumber = data.CompanyNumber, VatNumber = data.VatNumber, Email = data.Email, Phone = data.Phone, Street = data.Street, City = data.City, PostalCode = data.PostalCode, CountryCode = data.CountryCode };
            repository.AddImportedCustomer(customer);
            customers[key] = customer.Id;
        }
    }

    private static PohodaImportRow CreateRow(PohodaOrderData source, IReadOnlyDictionary<string, Guid> customers, IReadOnlyDictionary<string, Order> existing, IReadOnlySet<string> duplicateIds)
    {
        if (duplicateIds.Contains(source.ExternalId)) return Row(source, PohodaImportRowStatus.Error, "Soubor obsahuje objednávku vícekrát.");
        if (!string.Equals(source.OrderType, "receivedOrder", StringComparison.OrdinalIgnoreCase)) return Row(source, PohodaImportRowStatus.Error, "Nejde o přijatou objednávku.");
        if (string.IsNullOrWhiteSpace(source.Customer.Name)) return Row(source, PohodaImportRowStatus.Error, "Objednávka nemá název zákazníka.");
        if (source.ValueCzk < 0) return Row(source, PohodaImportRowStatus.Error, "Objednávka má zápornou celkovou hodnotu.");
        if (!existing.TryGetValue(source.ExternalId, out var order)) return Row(source, PohodaImportRowStatus.New, customers.ContainsKey(CustomerKey(source.Customer)) ? null : "Zákazník bude založen.");
        if (CustomerKey(order.Customer) != CustomerKey(source.Customer)) return Row(source, PohodaImportRowStatus.Warning, $"Zákazník se liší (v BCG: {order.Customer.Name}, v Pohodě: {source.Customer.Name}). Zákazník nebude změněn; ostatní údaje z Pohody budou aktualizovány.");
        return HasPohodaChanges(order, source) ? Row(source, PohodaImportRowStatus.Updated, "Údaje objednávky budou aktualizovány.") : Row(source, PohodaImportRowStatus.Unchanged, "Objednávka je beze změny.");
    }

    public static string CustomerKey(PohodaCustomerData customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.CompanyNumber)) return $"ico:{Normalize(customer.CompanyNumber)}";
        if (!string.IsNullOrWhiteSpace(customer.VatNumber)) return $"vat:{Normalize(customer.VatNumber)}";
        return $"name:{Normalize(customer.Name)}|country:{Normalize(customer.CountryCode ?? "")}";
    }

    private static string CustomerKey(BusinessPartner customer) => CustomerKey(new PohodaCustomerData(customer.Name, customer.CompanyNumber, customer.VatNumber, customer.Email, customer.Phone, customer.Street, customer.City, customer.PostalCode, customer.CountryCode));
    private static bool HasPohodaChanges(Order order, PohodaOrderData source) => order.PohodaOrderNumber != source.Number || order.Title != source.Title || order.OrderedOn != source.OrderedOn || order.RequestedDeliveryOn != source.DeliveryOn || order.ValueCzk != source.ValueCzk;
    private static void ApplyPohodaChanges(Order order, PohodaOrderData source) { order.PohodaOrderNumber = source.Number; order.Title = source.Title; order.OrderedOn = source.OrderedOn; order.RequestedDeliveryOn = source.DeliveryOn; order.ValueCzk = source.ValueCzk; }
    private static int Count(IEnumerable<PohodaImportRow> rows, PohodaImportRowStatus status) => rows.Count(x => x.Status == status);

    private static PohodaImportRow Row(PohodaOrderData order, PohodaImportRowStatus status, string? message) => new(order.ExternalId, order.Number, order.Title, order.Customer.Name, order.Customer.CompanyNumber, order.OrderedOn, order.DeliveryOn, order.ValueCzk, status, message);
    private static IReadOnlySet<string> DuplicateIds(IEnumerable<PohodaOrderData> orders) => orders.GroupBy(x => x.ExternalId, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
