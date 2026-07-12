using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PohodaOrderImportServiceTests
{
    [Fact]
    public async Task ImportCreatesMissingCustomerAndItsOrderInOneSave()
    {
        var source = new PohodaOrderData("12345678:42", "OBJ-42", "Objednávka", "receivedOrder", new PohodaCustomerData("Nový zákazník", "87654321", "CZ87654321", "info@example.cz", "123456789", "Hlavní 1", "Praha", "11000", "CZ"), new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 20), 12500m);
        var repository = new FakePohodaImportRepository();
        var service = new PohodaOrderImportService(new FakeParser(source), repository);

        var result = await service.ImportAsync(Stream.Null, CancellationToken.None);

        Assert.Equal(1, result.ImportedCount);
        Assert.NotNull(repository.Customer);
        Assert.Equal("87654321", repository.Customer.CompanyNumber);
        Assert.NotNull(repository.Order);
        Assert.Equal(repository.Customer.Id, repository.Order.CustomerId);
        Assert.Equal(1, repository.SaveCount);
    }

    private sealed class FakeParser(PohodaOrderData order) : IPohodaOrderXmlParser
    {
        public IReadOnlyList<PohodaOrderData> Parse(Stream xml) => [order];
    }

    private sealed class FakePohodaImportRepository : IPohodaImportRepository
    {
        public BusinessPartner? Customer { get; private set; }
        public Order? Order { get; private set; }
        public int SaveCount { get; private set; }
        public Task<IReadOnlyDictionary<string, Guid>> FindCustomersAsync(IEnumerable<PohodaCustomerData> customers, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyDictionary<string, Guid>>(new Dictionary<string, Guid>());
        public Task<IReadOnlySet<string>> FindExistingPohodaOrderIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        public Task<int> GetNextOrderSequenceAsync(int year, CancellationToken cancellationToken) => Task.FromResult(1);
        public void AddImportedCustomer(BusinessPartner customer) => Customer = customer;
        public void AddImportedOrder(Order order) => Order = order;
        public Task SaveImportAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
    }
}
