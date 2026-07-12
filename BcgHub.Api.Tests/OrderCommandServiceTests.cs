using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class OrderCommandServiceTests
{
    [Fact]
    public async Task CreateRejectsPartnerThatIsNotCustomer()
    {
        var repository = new FakeOrderRepository { Validation = new(false, true, true) };
        var service = new OrderCommandService(repository, repository);
        var request = ValidRequest();
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => service.CreateAsync(request, CancellationToken.None));
        Assert.Equal("Vybraný partner není zákazník.", exception.Message);
        Assert.Null(repository.AddedOrder);
    }

    [Fact]
    public async Task CreateBuildsCompleteWorkflowAndPersistsOnce()
    {
        var repository = new FakeOrderRepository();
        var service = new OrderCommandService(repository, repository);
        await service.CreateAsync(ValidRequest(), CancellationToken.None);
        Assert.NotNull(repository.AddedOrder);
        Assert.Equal(15, repository.AddedOrder.WorkflowSteps.Count);
        Assert.Equal(1, repository.SaveCount);
    }

    private static CreateOrderRequest ValidRequest() => new() { Number = "BCG-001", Title = "Test", CustomerId = Guid.NewGuid(), ValueCzk = 100, WeightKg = 5, VolumeM3 = 1 };

    private sealed class FakeOrderRepository : IOrderReadRepository, IOrderWriteRepository
    {
        public OrderReferenceValidation Validation { get; init; } = new(true, true, true);
        public Order? AddedOrder { get; private set; }
        public int SaveCount { get; private set; }
        public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<OrderReferenceValidation> ValidateReferencesAsync(Guid customerId, Guid? contactId, Guid? warehouseId, CancellationToken cancellationToken) => Task.FromResult(Validation);
        public Task AddAsync(Order order, CancellationToken cancellationToken) { AddedOrder = order; return Task.CompletedTask; }
        public Task<OrderWorkflowStep?> GetWorkflowStepAsync(Guid orderId, Guid stepId, CancellationToken cancellationToken) => Task.FromResult<OrderWorkflowStep?>(null);
        public void SetOriginalVersion(OrderWorkflowStep step, uint version) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
        public Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(new PagedResult<OrderListItem>([], page, pageSize, 0));
        public Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<OrderDetailDto?>(new(id, AddedOrder!.Number, null, AddedOrder.Title, AddedOrder.Status, new(AddedOrder.CustomerId, "Customer"), null, null, null, null, null, null, null, null, AddedOrder.ValueCzk, AddedOrder.WeightKg, AddedOrder.VolumeM3, null, [], []));
    }
}
