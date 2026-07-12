using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed class PohodaOrderImportService(IPohodaOrderXmlParser parser, IPohodaImportRepository repository) : IPohodaOrderImportService
{
    public async Task<PohodaImportPreview> PreviewAsync(Stream xml, CancellationToken cancellationToken)
    {
        var data = parser.Parse(xml);
        var customers = await repository.FindCustomersAsync(data.Select(x => x.Customer), cancellationToken);
        var existing = await repository.FindExistingPohodaOrderIdsAsync(data.Select(x => x.ExternalId), cancellationToken);
        var duplicateIds = DuplicateIds(data);
        var rows = data.Select(order => CreateRow(order, customers, existing, duplicateIds)).ToList();
        return new PohodaImportPreview(rows, rows.Count(x => x.Status == PohodaImportRowStatus.New), rows.Count(x => x.Status == PohodaImportRowStatus.Duplicate), rows.Count(x => x.Status == PohodaImportRowStatus.Error));
    }

    public async Task<PohodaImportResult> ImportAsync(Stream xml, CancellationToken cancellationToken)
    {
        var data = parser.Parse(xml);
        var customers = (await repository.FindCustomersAsync(data.Select(x => x.Customer), cancellationToken)).ToDictionary(StringComparer.OrdinalIgnoreCase);
        var existing = await repository.FindExistingPohodaOrderIdsAsync(data.Select(x => x.ExternalId), cancellationToken);
        var duplicateIds = DuplicateIds(data);
        var valid = data.Where(x => CreateRow(x, customers, existing, duplicateIds).Status == PohodaImportRowStatus.New).ToList();
        AddMissingCustomers(valid, customers);
        var sequences = new Dictionary<int, int>();
        var pending = 0;
        foreach (var source in valid)
        {
            var year = source.OrderedOn?.Year ?? DateTime.UtcNow.Year;
            if (!sequences.TryGetValue(year, out var sequence)) sequence = await repository.GetNextOrderSequenceAsync(year, cancellationToken);
            sequences[year] = sequence + 1;
            var order = new Order { Number = $"BCG_{year}{sequence:0000}", PohodaOrderId = source.ExternalId, PohodaOrderNumber = source.Number, Title = source.Title, CustomerId = customers[CustomerKey(source.Customer)], OrderedOn = source.OrderedOn, RequestedDeliveryOn = source.DeliveryOn, ValueCzk = source.ValueCzk };
            order.WorkflowSteps = WorkflowCatalog.CreateSteps(order.Id);
            var created = order.WorkflowSteps.Single(x => x.Type == WorkflowStepType.OrderCreatedInPohoda); created.Status = WorkflowStepStatus.Completed; created.CompletedAtUtc = DateTime.UtcNow;
            repository.AddImportedOrder(order);
            pending++;
            if (pending < 100) continue;
            await repository.SaveImportAsync(cancellationToken);
            pending = 0;
        }
        if (pending > 0) await repository.SaveImportAsync(cancellationToken);
        var duplicateCount = data.Count(x => existing.Contains(x.ExternalId));
        return new PohodaImportResult(valid.Count, duplicateCount, data.Count - valid.Count - duplicateCount);
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

    private static PohodaImportRow CreateRow(PohodaOrderData order, IReadOnlyDictionary<string, Guid> customers, IReadOnlySet<string> existing, IReadOnlySet<string> duplicateIds)
    {
        if (existing.Contains(order.ExternalId)) return Row(order, PohodaImportRowStatus.Duplicate, "Objednávka již byla importována.");
        if (duplicateIds.Contains(order.ExternalId)) return Row(order, PohodaImportRowStatus.Error, "Soubor obsahuje objednávku vícekrát.");
        if (!string.Equals(order.OrderType, "receivedOrder", StringComparison.OrdinalIgnoreCase)) return Row(order, PohodaImportRowStatus.Error, "Nejde o přijatou objednávku.");
        if (string.IsNullOrWhiteSpace(order.Customer.Name)) return Row(order, PohodaImportRowStatus.Error, "Objednávka nemá název zákazníka.");
        if (order.ValueCzk < 0) return Row(order, PohodaImportRowStatus.Error, "Objednávka má zápornou celkovou hodnotu.");
        return Row(order, PohodaImportRowStatus.New, customers.ContainsKey(CustomerKey(order.Customer)) ? null : "Zákazník bude založen.");
    }

    public static string CustomerKey(PohodaCustomerData customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.CompanyNumber)) return $"ico:{Normalize(customer.CompanyNumber)}";
        if (!string.IsNullOrWhiteSpace(customer.VatNumber)) return $"vat:{Normalize(customer.VatNumber)}";
        return $"name:{Normalize(customer.Name)}|country:{Normalize(customer.CountryCode ?? "")}";
    }

    private static PohodaImportRow Row(PohodaOrderData order, PohodaImportRowStatus status, string? message) => new(order.ExternalId, order.Number, order.Title, order.Customer.Name, order.Customer.CompanyNumber, order.OrderedOn, order.DeliveryOn, order.ValueCzk, status, message);
    private static IReadOnlySet<string> DuplicateIds(IEnumerable<PohodaOrderData> orders) => orders.GroupBy(x => x.ExternalId, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static string Normalize(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
