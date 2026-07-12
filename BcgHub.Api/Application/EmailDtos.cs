using System.ComponentModel.DataAnnotations;

namespace BcgHub.Api.Application;

public sealed record EmailSettingsDto(string Provider, string ImapServer, int ImapPort, bool ImapUseSsl, string ImapUsername, bool HasImapPassword, string SmtpServer, int SmtpPort, bool SmtpUseSsl, string SmtpUsername, bool HasSmtpPassword, string SenderAddress, string? SenderName, bool IsActive, bool IsMicrosoftConnected, string? MicrosoftMailboxAddress);
public sealed record SaveEmailSettingsRequest([Required, StringLength(300)] string ImapServer, [Range(1, 65535)] int ImapPort, bool ImapUseSsl, [Required, EmailAddress, StringLength(320)] string ImapUsername, [StringLength(1000)] string ImapPassword, [Required, StringLength(300)] string SmtpServer, [Range(1, 65535)] int SmtpPort, bool SmtpUseSsl, [Required, StringLength(320)] string SmtpUsername, [StringLength(1000)] string SmtpPassword, [Required, EmailAddress, StringLength(320)] string SenderAddress, [StringLength(300)] string? SenderName, bool IsActive);
public sealed record SetEmailProviderRequest([Required] string Provider);
public sealed record EmailMessageDto(Guid Id, string Direction, string FromAddress, string? FromName, string ToAddress, string Subject, string? BodyText, string? BodyHtml, DateTime OccurredAtUtc, bool IsRead, bool HasAttachments, Guid? BusinessPartnerId, string? BusinessPartnerName, Guid? OrderId, string? OrderNumber, uint Version);
public sealed record LinkEmailRequest(Guid? BusinessPartnerId, Guid? OrderId, uint Version);
public sealed record EmailOrderOptionDto(Guid Id, string Number, string Title, string CustomerName);
public sealed record EmailOrderOptionsDto(IReadOnlyList<EmailOrderOptionDto> Suggested, IReadOnlyList<EmailOrderOptionDto> Other);
public sealed record EmailActionContextDto(string SenderType, string MatchedBy, PartnerReference? Partner);
public sealed record EmailTransportQuoteContextDto(PartnerReference Carrier, Guid? SuggestedOrderId, IReadOnlyList<EmailOrderOptionDto> Orders);
public sealed record CreateEmailTransportQuoteRequest(Guid OrderId, [Range(0, 9999999999999999d)] decimal Price, [Required, RegularExpression("^[A-Za-z]{3}$")] string Currency, DateOnly? PickupOn, DateOnly? DeliveryOn, [StringLength(5000)] string? Notes);
public sealed record EmailSyncResultDto(int ImportedCount);
public sealed record SendEmailRequest([Required, EmailAddress, StringLength(320)] string ToAddress, [EmailAddress, StringLength(320)] string? CcAddress, [Required, StringLength(1000)] string Subject, [Required] string BodyHtml, Guid? ReplyToEmailId, Guid? BusinessPartnerId, Guid? OrderId);
public sealed record EmailTemplateDto(Guid Id, string Name, string Subject, string BodyHtml, uint Version);
public sealed record SaveEmailTemplateRequest([Required, StringLength(200)] string Name, [StringLength(1000)] string Subject, [Required] string BodyHtml, uint Version);
public sealed record TransportInquiryCarrierDto(Guid Id, string Name, string Email);
public sealed record TransportInquiryContextDto(string OrderNumber, IReadOnlyList<TransportInquiryCarrierDto> Carriers);
public sealed record SendTransportInquiryRequest([Required] string TransportType, IReadOnlyList<Guid> CarrierIds, [Required, StringLength(1000)] string Subject, [Required] string BodyHtml);
public sealed record SendTransportInquiryResult(int SentCount);
