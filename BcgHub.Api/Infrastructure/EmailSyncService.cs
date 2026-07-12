using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSyncService(BcgHubDbContext db, CurrentUserAccessor currentUser, EmailSettingsService settingsService, IEmailSyncLock syncLock) : IEmailSyncService
{
    public async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        await using var lease = await syncLock.AcquireAsync(userId, cancellationToken);
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == userId && x.IsActive, cancellationToken) ?? throw new DomainValidationException("Nejdříve nastavte aktivní e-mailovou schránku.");
        using var client = new ImapClient();
        await client.ConnectAsync(settings.ImapServer, settings.ImapPort, settings.ImapUseSsl, cancellationToken);
        await client.AuthenticateAsync(settings.ImapUsername, settingsService.Unprotect(settings), cancellationToken);
        var inbox = client.Inbox ?? throw new InvalidOperationException("IMAP server neposkytl složku INBOX.");
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var uids = await inbox.SearchAsync(SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-30)), cancellationToken);
        var existing = await db.EmailMessages.Where(x => x.UserAccountId == userId).Select(x => x.ExternalId).ToHashSetAsync(cancellationToken);
        var matcher = await EmailMatcher.LoadAsync(db, cancellationToken);
        var imported = 0;
        foreach (var uid in uids.OrderByDescending(x => x.Id).Take(200))
        {
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            var externalId = string.IsNullOrWhiteSpace(message.MessageId) ? $"INBOX:{uid.Id}" : message.MessageId;
            if (existing.Contains(externalId)) continue;
            var from = message.From.Mailboxes.FirstOrDefault();
            var match = matcher.Match(from?.Address, message.Subject);
            db.EmailMessages.Add(new EmailMessage { UserAccountId = userId, Direction = EmailDirection.Inbound, ExternalId = externalId, ImapUid = uid.Id, FromAddress = from?.Address ?? "", FromName = from?.Name, ToAddress = string.Join(", ", message.To.Mailboxes.Select(x => x.Address)), CcAddress = string.Join(", ", message.Cc.Mailboxes.Select(x => x.Address)), Subject = message.Subject ?? "(bez předmětu)", BodyText = message.TextBody, BodyHtml = message.HtmlBody, OccurredAtUtc = message.Date.UtcDateTime, HasAttachments = message.Attachments.Any(), BusinessPartnerId = match.BusinessPartnerId, OrderId = match.OrderId });
            existing.Add(externalId);
            imported++;
        }
        await db.SaveChangesAsync(cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        return imported;
    }
}

internal sealed class EmailMatcher(IReadOnlyList<EmailMatcher.PartnerAddress> addresses, IReadOnlyList<EmailMatcher.OrderNumber> orders)
{
    public sealed record MatchResult(Guid? BusinessPartnerId, Guid? OrderId);
    public sealed record PartnerAddress(Guid PartnerId, string Email);
    public sealed record OrderNumber(Guid OrderId, string Number);

    public static async Task<EmailMatcher> LoadAsync(BcgHubDbContext db, CancellationToken cancellationToken)
    {
        var partnerAddresses = await db.BusinessPartners.AsNoTracking().Where(x => x.Email != null).Select(x => new PartnerAddress(x.Id, x.Email!)).Concat(db.ContactPeople.AsNoTracking().Where(x => x.Email != null).Select(x => new PartnerAddress(x.BusinessPartnerId, x.Email!))).ToListAsync(cancellationToken);
        var orderNumbers = await db.Orders.AsNoTracking().Select(x => new OrderNumber(x.Id, x.Number)).ToListAsync(cancellationToken);
        return new EmailMatcher(partnerAddresses, orderNumbers);
    }

    public MatchResult Match(string? sender, string? subject)
    {
        var partnerId = addresses.FirstOrDefault(x => string.Equals(x.Email, sender, StringComparison.OrdinalIgnoreCase))?.PartnerId;
        var orderId = orders.FirstOrDefault(x => subject?.Contains(x.Number, StringComparison.OrdinalIgnoreCase) == true)?.OrderId;
        return new MatchResult(partnerId, orderId);
    }
}
