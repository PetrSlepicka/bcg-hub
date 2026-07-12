using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MimeKit.Utils;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSender(BcgHubDbContext db, CurrentUserAccessor currentUser, EmailSettingsService settingsService) : IEmailSender
{
    public async Task<EmailMessageDto> SendAsync(SendEmailRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId && x.IsActive, cancellationToken) ?? throw new DomainValidationException("Nejdříve nastavte aktivní e-mailovou schránku.");
        var replyTo = request.ReplyToEmailId.HasValue ? await db.EmailMessages.SingleOrDefaultAsync(x => x.Id == request.ReplyToEmailId && x.UserAccountId == currentUser.UserId, cancellationToken) : null;
        if (request.ReplyToEmailId.HasValue && replyTo is null) throw new DomainValidationException("Původní e-mail nebyl nalezen.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SenderName ?? "", settings.SenderAddress));
        message.To.Add(MailboxAddress.Parse(request.ToAddress));
        if (!string.IsNullOrWhiteSpace(request.CcAddress)) message.Cc.Add(MailboxAddress.Parse(request.CcAddress));
        message.Subject = request.Subject.Trim();
        message.MessageId = MimeUtils.GenerateMessageId();
        message.Body = new BodyBuilder { HtmlBody = request.BodyHtml }.ToMessageBody();
        if (replyTo is not null && replyTo.ExternalId.StartsWith('<') && replyTo.ExternalId.EndsWith('>')) { message.InReplyTo = replyTo.ExternalId; message.References.Add(replyTo.ExternalId); }

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.SmtpServer, settings.SmtpPort, SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(settings.SmtpUsername, settingsService.UnprotectSmtpPassword(settings), cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        var sent = new EmailMessage { UserAccountId = currentUser.UserId, Direction = EmailDirection.Outbound, ExternalId = message.MessageId, Mailbox = "Sent", FromAddress = settings.SenderAddress, FromName = settings.SenderName, ToAddress = request.ToAddress.Trim(), CcAddress = string.IsNullOrWhiteSpace(request.CcAddress) ? null : request.CcAddress.Trim(), Subject = message.Subject, BodyHtml = request.BodyHtml, OccurredAtUtc = DateTime.UtcNow, IsRead = true, BusinessPartnerId = request.BusinessPartnerId ?? replyTo?.BusinessPartnerId, OrderId = request.OrderId ?? replyTo?.OrderId };
        db.EmailMessages.Add(sent);
        await db.SaveChangesAsync(cancellationToken);
        return EmailQueryService.Map(sent);
    }
}
