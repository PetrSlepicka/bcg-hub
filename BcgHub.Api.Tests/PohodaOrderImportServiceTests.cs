using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PohodaOrderImportServiceTests
{
    [Fact]
    public async Task RepeatedUnchangedImportDoesNotCreateOrSaveAnythingAgain()
    {
        var source = OrderData();
        var repository = new FakePohodaImportRepository();

        var first = await ImportAsync(source, repository);
        var second = await ImportAsync(source, repository);

        Assert.Equal(1, first.ImportedCount);
        Assert.Equal(1, second.UnchangedCount);
        Assert.Single(repository.Orders);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task RepeatedImportUpdatesOnlyFieldsOwnedByPohoda()
    {
        var repository = new FakePohodaImportRepository();
        await ImportAsync(OrderData(), repository);
        var order = Assert.Single(repository.Orders);
        order.Status = OrderStatus.InProgress;
        order.PlannedDeliveryOn = new DateOnly(2026, 7, 30);
        order.WarehouseInstructions = "Ruční instrukce";
        var changed = OrderData(number: "OBJ-42-A", title: "Doplněná objednávka", orderedOn: new DateOnly(2026, 7, 13), deliveryOn: new DateOnly(2026, 7, 21), valueCzk: 15000m);

        var result = await ImportAsync(changed, repository);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Single(repository.Orders);
        Assert.Equal("OBJ-42-A", order.PohodaOrderNumber);
        Assert.Equal("Doplněná objednávka", order.Title);
        Assert.Equal(new DateOnly(2026, 7, 13), order.OrderedOn);
        Assert.Equal(new DateOnly(2026, 7, 21), order.RequestedDeliveryOn);
        Assert.Equal(15000m, order.ValueCzk);
        Assert.Equal(OrderStatus.InProgress, order.Status);
        Assert.Equal(new DateOnly(2026, 7, 30), order.PlannedDeliveryOn);
        Assert.Equal("Ruční instrukce", order.WarehouseInstructions);
    }

    [Fact]
    public async Task ChangedCustomerIsReportedButNotApplied()
    {
        var repository = new FakePohodaImportRepository();
        await ImportAsync(OrderData(), repository);
        var order = Assert.Single(repository.Orders);
        var originalCustomerId = order.CustomerId;
        var changedCustomer = new PohodaCustomerData("Jiný zákazník", "99999999", "CZ99999999", "other@example.cz", null, null, null, null, "CZ");
        var changed = OrderData(title: "Aktualizovaný název", customer: changedCustomer);
        var service = new PohodaOrderImportService(new FakeParser(changed), repository, NullLogger<PohodaOrderImportService>.Instance);

        var preview = await service.PreviewAsync(Stream.Null, CancellationToken.None);
        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        var row = Assert.Single(preview.Rows);
        Assert.Equal(PohodaImportRowStatus.Warning, row.Status);
        Assert.Contains("Zákazník nebude změněn", row.Message);
        Assert.Equal(1, result.WarningCount);
        Assert.Equal(originalCustomerId, order.CustomerId);
        Assert.Equal("Nový zákazník", order.Customer.Name);
        Assert.Equal("Aktualizovaný název", order.Title);
        Assert.Single(repository.Customers);
    }

    private static async Task<PohodaImportResult> ImportAsync(PohodaOrderData source, FakePohodaImportRepository repository) => await new PohodaOrderImportService(new FakeParser(source), repository, NullLogger<PohodaOrderImportService>.Instance).ImportAsync(Stream.Null, CancellationToken.None);
    private static PohodaOrderData OrderData(string? number = "OBJ-42", string title = "Objednávka", DateOnly? orderedOn = null, DateOnly? deliveryOn = null, decimal valueCzk = 12500m, PohodaCustomerData? customer = null) => new("12345678:42", number, title, "receivedOrder", customer ?? new PohodaCustomerData("Nový zákazník", "87654321", "CZ87654321", "info@example.cz", "123456789", "Hlavní 1", "Praha", "11000", "CZ"), orderedOn ?? new DateOnly(2026, 7, 12), deliveryOn ?? new DateOnly(2026, 7, 20), valueCzk);

    private sealed class FakeParser(params PohodaOrderData[] orders) : IPohodaOrderXmlParser
    {
        public IReadOnlyList<PohodaOrderData> Parse(Stream xml, string? accountingUnitFallback = null, bool allowEmpty = false) => orders;
    }

    private sealed class FakePohodaImportRepository : IPohodaImportRepository
    {
        public List<BusinessPartner> Customers { get; } = [];
        public List<Order> Orders { get; } = [];
        public int SaveCount { get; private set; }

        public Task<IReadOnlyDictionary<string, Guid>> FindCustomersAsync(IEnumerable<PohodaCustomerData> customers, CancellationToken cancellationToken)
        {
            var requested = customers.Select(PohodaOrderImportService.CustomerKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var found = Customers.Where(x => requested.Contains(PohodaOrderImportService.CustomerKey(new PohodaCustomerData(x.Name, x.CompanyNumber, x.VatNumber, x.Email, x.Phone, x.Street, x.City, x.PostalCode, x.CountryCode)))).ToDictionary(x => PohodaOrderImportService.CustomerKey(new PohodaCustomerData(x.Name, x.CompanyNumber, x.VatNumber, x.Email, x.Phone, x.Street, x.City, x.PostalCode, x.CountryCode)), x => x.Id, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyDictionary<string, Guid>>(found);
        }

        public Task<IReadOnlyDictionary<string, Order>> FindExistingPohodaOrdersAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken)
        {
            var requested = externalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var found = Orders.Where(x => x.PohodaOrderId is not null && requested.Contains(x.PohodaOrderId)).ToDictionary(x => x.PohodaOrderId!, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyDictionary<string, Order>>(found);
        }

        public Task<int> GetNextOrderSequenceAsync(int year, CancellationToken cancellationToken) => Task.FromResult(Orders.Count(x => x.Number.StartsWith($"BCG_{year}")) + 1);
        public void AddImportedCustomer(BusinessPartner customer) => Customers.Add(customer);
        public void AddImportedOrder(Order order) { order.Customer = Customers.Single(x => x.Id == order.CustomerId); Orders.Add(order); }
        public Task SaveImportAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
    }
}
