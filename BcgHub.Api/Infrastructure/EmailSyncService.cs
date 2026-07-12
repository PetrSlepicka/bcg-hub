using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSyncService(BcgHubDbContext db, CurrentUserAccessor currentUser, EmailSettingsService settingsService, IEmailSyncLock syncLock, IFileStorage fileStorage, IEmailProcessor emailProcessor, IMicrosoftGraphMailService microsoftGraph) : IEmailSyncService
{
    public async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        return await SyncUserAsync(currentUser.UserId, cancellationToken);
    }

    public async Task<int> SyncUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var lease = await syncLock.AcquireAsync(userId, cancellationToken);
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == userId && x.IsActive, cancellationToken) ?? throw new DomainValidationException("Nejdříve nastavte aktivní e-mailovou schránku.");
        if (settings.Provider == EmailProvider.MicrosoftGraph) return await microsoftGraph.SyncAsync(settings, cancellationToken);
        using var client = new ImapClient();
        await client.ConnectAsync(settings.ImapServer, settings.ImapPort, settings.ImapUseSsl, cancellationToken);
        await client.AuthenticateAsync(settings.ImapUsername, settingsService.Unprotect(settings), cancellationToken);
        var inbox = client.Inbox ?? throw new InvalidOperationException("IMAP server neposkytl složku INBOX.");
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var uids = await inbox.SearchAsync(SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-30)), cancellationToken);
        var existing = await db.EmailMessages.Where(x => x.UserAccountId == userId).Select(x => x.ExternalId).ToHashSetAsync(cancellationToken);
        var imported = 0;
        foreach (var uid in uids.OrderByDescending(x => x.Id).Take(200))
        {
            var message = await inbox.GetMessageAsync(uid, cancellationToken);
            var externalId = string.IsNullOrWhiteSpace(message.MessageId) ? $"INBOX:{uid.Id}" : message.MessageId;
            if (existing.Contains(externalId)) continue;
            var from = message.From.Mailboxes.FirstOrDefault();
            var email = new EmailMessage { UserAccountId = userId, Direction = EmailDirection.Inbound, ExternalId = externalId, ImapUid = uid.Id, FromAddress = from?.Address ?? "", FromName = from?.Name, ToAddress = string.Join(", ", message.To.Mailboxes.Select(x => x.Address)), CcAddress = string.Join(", ", message.Cc.Mailboxes.Select(x => x.Address)), Subject = message.Subject ?? "(bez předmětu)", BodyText = message.TextBody, BodyHtml = message.HtmlBody, OccurredAtUtc = message.Date.UtcDateTime, HasAttachments = message.Attachments.Any() };
            await emailProcessor.ProcessAsync(email, cancellationToken);
            db.EmailMessages.Add(email);
            foreach (var attachment in message.Attachments) await ImportAttachmentAsync(email, attachment, cancellationToken);
            existing.Add(externalId);
            imported++;
        }
        await db.SaveChangesAsync(cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        return imported;
    }

    private async Task ImportAttachmentAsync(EmailMessage email, MimeEntity entity, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        string fileName;
        string contentType;
        if (entity is MimePart part && part.Content is { } content) { await content.DecodeToAsync(stream, cancellationToken); fileName = string.IsNullOrWhiteSpace(part.FileName) ? "priloha.bin" : part.FileName; contentType = part.ContentType.MimeType; }
        else if (entity is MessagePart messagePart && messagePart.Message is { } message) { await message.WriteToAsync(stream, cancellationToken); fileName = messagePart.ContentDisposition?.Parameters["filename"] ?? "priloha.eml"; contentType = "message/rfc822"; }
        else return;
        if (stream.Length > 2 * 1024 * 1024) return;
        stream.Position = 0;
        var key = await fileStorage.SaveAsync(fileName, stream, cancellationToken);
        db.Attachments.Add(new Attachment { EmailMessageId = email.Id, FileName = fileName, ContentType = contentType, Size = stream.Length, StorageKey = key });
    }
}
