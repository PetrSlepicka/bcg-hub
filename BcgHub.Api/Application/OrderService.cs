using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed class OrderQueryService(IOrderReadRepository repository) : IOrderQueryService
{
    public Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken) => repository.GetListAsync(search, sortBy, descending, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), cancellationToken);
    public Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken) => repository.GetDetailAsync(id, cancellationToken);
}

public sealed class OrderCommandService(IOrderWriteRepository repository, IOrderReadRepository readRepository) : IOrderCommandService
{
    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var number = request.Number.Trim();
        var title = request.Title.Trim();
        if (number.Length == 0) throw new DomainValidationException("Číslo zakázky je povinné.");
        if (title.Length == 0) throw new DomainValidationException("Název zakázky je povinný.");
        if (request.CustomerId == Guid.Empty) throw new DomainValidationException("Zákazník je povinný.");
        if (await repository.NumberExistsAsync(number, cancellationToken)) throw new DomainValidationException("Zakázka se stejným číslem již existuje.");

        var references = await repository.ValidateReferencesAsync(request.CustomerId, request.CustomerContactId, request.WarehouseId, cancellationToken);
        if (!references.CustomerIsValid) throw new DomainValidationException("Vybraný partner není zákazník.");
        if (!references.ContactMatchesCustomer) throw new DomainValidationException("Kontaktní osoba nepatří vybranému zákazníkovi.");
        if (!references.WarehouseIsValid) throw new DomainValidationException("Vybraný partner není sklad.");

        var order = new Order { Number = number, PohodaOrderNumber = request.PohodaOrderNumber?.Trim(), Title = title, CustomerId = request.CustomerId, CustomerContactId = request.CustomerContactId, WarehouseId = request.WarehouseId, OrderedOn = request.OrderedOn, RequestedDeliveryOn = request.RequestedDeliveryOn, ValueCzk = request.ValueCzk, WeightKg = request.WeightKg, VolumeM3 = request.VolumeM3, WarehouseInstructions = request.WarehouseInstructions?.Trim() };
        order.WorkflowSteps = WorkflowCatalog.CreateSteps(order.Id);
        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return (await readRepository.GetDetailAsync(order.Id, cancellationToken))!;
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
}
