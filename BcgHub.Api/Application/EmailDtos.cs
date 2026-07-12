using System.ComponentModel.DataAnnotations;

namespace BcgHub.Api.Application;

public sealed record EmailSettingsDto(string ImapServer, int ImapPort, bool ImapUseSsl, string ImapUsername, bool HasPassword, bool IsActive);
public sealed record SaveEmailSettingsRequest([Required, StringLength(300)] string ImapServer, [Range(1, 65535)] int ImapPort, bool ImapUseSsl, [Required, EmailAddress, StringLength(320)] string ImapUsername, [StringLength(1000)] string ImapPassword, bool IsActive);
public sealed record EmailMessageDto(Guid Id, string Direction, string FromAddress, string? FromName, string ToAddress, string Subject, string? BodyText, string? BodyHtml, DateTime OccurredAtUtc, bool IsRead, bool HasAttachments, Guid? BusinessPartnerId, string? BusinessPartnerName, Guid? OrderId, string? OrderNumber, uint Version);
public sealed record LinkEmailRequest(Guid? BusinessPartnerId, Guid? OrderId, uint Version);
public sealed record EmailSyncResultDto(int ImportedCount);
