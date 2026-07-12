using BcgHub.Api.Domain;

namespace BcgHub.Api.Application;

public sealed record OrderReferenceValidation(bool CustomerIsValid, bool ContactMatchesCustomer, bool WarehouseIsValid, bool CarrierIsValid, bool CustomsDeclarantIsValid);

public interface IOrderReadRepository
{
    Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, Guid? customerId, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderWriteRepository
{
    Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken);
    Task<OrderReferenceValidation> ValidateReferencesAsync(Guid customerId, Guid? contactId, Guid? warehouseId, Guid? carrierId, Guid? customsDeclarantId, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task<OrderWorkflowStep?> GetWorkflowStepAsync(Guid orderId, Guid stepId, CancellationToken cancellationToken);
    Task<Order?> GetOrderAsync(Guid id, CancellationToken cancellationToken);
    void SetOriginalVersion(Order order, uint version);
    void Remove(Order order);
    Task<TransportQuote?> GetQuoteAsync(Guid orderId, Guid quoteId, CancellationToken cancellationToken);
    void SetOriginalVersion(TransportQuote quote, uint version);
    Task<bool> IsCarrierAsync(Guid carrierId, CancellationToken cancellationToken);
    Task ClearSelectedQuoteAsync(Guid orderId, Guid? exceptId, CancellationToken cancellationToken);
    void AddQuote(TransportQuote quote);
    void RemoveQuote(TransportQuote quote);
    void SetOriginalVersion(OrderWorkflowStep step, uint version);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IOrderQueryService
{
    Task<PagedResult<OrderListItem>> GetListAsync(string? search, string sortBy, bool descending, int page, int pageSize, Guid? customerId, CancellationToken cancellationToken);
    Task<OrderDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
}

public interface IOrderCommandService
{
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<OrderDetailDto?> UpdateAsync(Guid id, UpdateOrderRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken);
    Task<WorkflowStepDto?> UpdateStepAsync(Guid orderId, Guid stepId, UpdateWorkflowStepRequest request, CancellationToken cancellationToken);
    Task<TransportQuoteDto?> AddQuoteAsync(Guid orderId, SaveTransportQuoteRequest request, CancellationToken cancellationToken);
    Task<TransportQuoteDto?> UpdateQuoteAsync(Guid orderId, Guid quoteId, SaveTransportQuoteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteQuoteAsync(Guid orderId, Guid quoteId, uint version, CancellationToken cancellationToken);
}

public interface IPohodaImportRepository
{
    Task<IReadOnlyDictionary<string, Guid>> FindCustomersAsync(IEnumerable<PohodaCustomerData> customers, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> FindExistingPohodaOrderIdsAsync(IEnumerable<string> externalIds, CancellationToken cancellationToken);
    Task<int> GetNextOrderSequenceAsync(int year, CancellationToken cancellationToken);
    void AddImportedCustomer(BusinessPartner customer);
    void AddImportedOrder(Order order);
    Task SaveImportAsync(CancellationToken cancellationToken);
}

public interface IPohodaOrderImportService
{
    Task<PohodaImportPreview> PreviewAsync(Stream xml, CancellationToken cancellationToken);
    Task<PohodaImportResult> ImportAsync(Stream xml, CancellationToken cancellationToken);
}

public interface IPartnerService
{
    Task<PagedResult<PartnerListItem>> GetListAsync(PartnerType? type, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<PartnerDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<PartnerDetailDto> CreateAsync(SavePartnerRequest request, CancellationToken cancellationToken);
    Task<PartnerDetailDto?> UpdateAsync(Guid id, SavePartnerRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken);
    Task<ContactPersonDto?> AddContactAsync(Guid partnerId, SaveContactPersonRequest request, CancellationToken cancellationToken);
    Task<ContactPersonDto?> UpdateContactAsync(Guid partnerId, Guid contactId, SaveContactPersonRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteContactAsync(Guid partnerId, Guid contactId, uint version, CancellationToken cancellationToken);
}

public interface IAuthService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken);
}

public interface IUserManagementService
{
    Task<IReadOnlyList<ManagedUserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<CreatedManagedUserDto> CreateAsync(CreateManagedUserRequest request, CancellationToken cancellationToken);
    Task<ManagedUserDto?> UpdateAsync(Guid id, UpdateManagedUserRequest request, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken);
}

public interface IUserPasswordGenerator
{
    string CreatePassword();
}

public interface IEmailSettingsService
{
    Task<EmailSettingsDto?> GetAsync(CancellationToken cancellationToken);
    Task<EmailSettingsDto> SaveAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken);
    Task<EmailSettingsDto> SetProviderAsync(string provider, CancellationToken cancellationToken);
}

public interface IMicrosoftGraphConnectionService
{
    string CreateAuthorizationUrl(string redirectUri, string returnUrl);
    Task<string> CompleteAuthorizationAsync(string code, string state, string redirectUri, CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}

public interface IMicrosoftGraphMailService
{
    Task<int> SyncAsync(EmailAccountSettings settings, CancellationToken cancellationToken);
    Task<EmailMessageDto> SendAsync(EmailAccountSettings settings, SendEmailRequest request, CancellationToken cancellationToken);
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

public interface IEmailProcessor
{
    Task ProcessAsync(EmailMessage email, CancellationToken cancellationToken);
    Task<EmailOrderOptionsDto> GetOrderOptionsAsync(EmailMessage email, CancellationToken cancellationToken);
    Task<EmailActionContextDto> GetActionContextAsync(EmailMessage email, CancellationToken cancellationToken);
}

public enum EmailSenderMatchKind { None, Address, Domain, Ambiguous }
public sealed record EmailSenderMatch(BusinessPartner? Partner, EmailSenderMatchKind Kind);

public interface IEmailSenderResolver
{
    Task<EmailSenderMatch> ResolveAsync(string address, CancellationToken cancellationToken);
}

public interface IEmailSender
{
    Task<EmailMessageDto> SendAsync(SendEmailRequest request, CancellationToken cancellationToken);
}

public interface ITransportInquiryService
{
    Task<TransportInquiryContextDto?> GetContextAsync(Guid orderId, string transportType, CancellationToken cancellationToken);
    Task<SendTransportInquiryResult?> SendAsync(Guid orderId, SendTransportInquiryRequest request, CancellationToken cancellationToken);
}

public interface IEmailTransportQuoteService
{
    Task<EmailTransportQuoteContextDto?> GetContextAsync(Guid emailId, CancellationToken cancellationToken);
    Task<TransportQuoteDto?> CreateAsync(Guid emailId, CreateEmailTransportQuoteRequest request, CancellationToken cancellationToken);
}

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplateDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<EmailTemplateDto> CreateAsync(SaveEmailTemplateRequest request, CancellationToken cancellationToken);
    Task<EmailTemplateDto?> UpdateAsync(Guid id, SaveEmailTemplateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken);
}

public interface IEntityResourceService
{
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken);
    Task<CommentDto> AddCommentAsync(ResourceOwnerType ownerType, Guid ownerId, SaveCommentRequest request, CancellationToken cancellationToken);
    Task<CommentDto?> UpdateCommentAsync(Guid commentId, SaveCommentRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteCommentAsync(Guid commentId, uint version, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttachmentDto>> GetAttachmentsAsync(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken);
    Task<AttachmentDto> AddAttachmentAsync(ResourceOwnerType ownerType, Guid ownerId, string fileName, string contentType, long size, Stream content, CancellationToken cancellationToken);
    Task<StoredFile?> OpenAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken);
    Task<bool> DeleteAttachmentAsync(Guid attachmentId, uint version, CancellationToken cancellationToken);
}

public interface IComplaintService
{
    Task<IReadOnlyList<ComplaintListItem>> GetListAsync(CancellationToken cancellationToken);
    Task<ComplaintDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<ComplaintDetailDto> CreateAsync(SaveComplaintRequest request, CancellationToken cancellationToken);
    Task<ComplaintDetailDto?> UpdateAsync(Guid id, SaveComplaintRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken);
}

public interface ICommunicationService
{
    Task<PagedResult<CommunicationDto>> GetListAsync(Guid? partnerId, Guid? orderId, int page, int pageSize, CancellationToken cancellationToken);
    Task<CommunicationDto> CreateAsync(SaveCommunicationRequest request, CancellationToken cancellationToken);
    Task<CommunicationDto?> UpdateAsync(Guid id, SaveCommunicationRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, uint version, CancellationToken cancellationToken);
}
