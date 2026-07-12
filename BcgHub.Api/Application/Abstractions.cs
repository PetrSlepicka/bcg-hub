using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed record OrderReferenceValidation(bool CustomerIsValid, bool ContactMatchesCustomer, bool WarehouseIsValid);

public interface IOrderReadRepository
{
    Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderWriteRepository
{
    Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken);
    Task<OrderReferenceValidation> ValidateReferencesAsync(Guid customerId, Guid? contactId, Guid? warehouseId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<OrderWorkflowStep?> GetWorkflowStepAsync(Guid orderId, Guid stepId, CancellationToken cancellationToken);
    void SetOriginalVersion(OrderWorkflowStep step, uint version);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrderQueryService
{
    Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderCommandService
{
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<WorkflowStepDto?> UpdateStepAsync(Guid orderId, Guid stepId, UpdateWorkflowStepRequest request, CancellationToken cancellationToken);
}

public interface IPartnerQueryService
{
    Task<PagedResult<PartnerListItem>> GetListAsync(PartnerType? type, string? search, int page, int pageSize, CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}

public interface IEmailSettingsService
{
    Task<EmailSettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<EmailSettingsDto> SaveAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken);
}

public interface IEmailQueryService
{
    Task<PagedResult<EmailMessageDto>> GetListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<EmailMessageDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
}

public interface IEmailCommandService
{
    Task<EmailMessageDto?> LinkAsync(Guid id, LinkEmailRequest request, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken);
}

public interface IEmailSyncService
{
    Task<int> SyncAsync(CancellationToken cancellationToken);
}
