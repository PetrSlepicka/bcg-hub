using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class MicrosoftGraphMailService(BcgHubDbContext db, MicrosoftGraphConnectionService connection, IHttpClientFactory httpClientFactory, IFileStorage fileStorage, IEmailProcessor emailProcessor) : IMicrosoftGraphMailService
{
    private const string InitialDeltaUrl = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$select=id,internetMessageId,from,toRecipients,ccRecipients,subject,body,receivedDateTime,isRead,hasAttachments&$top=50";

    public async Task<int> SyncAsync(EmailAccountSettings settings, CancellationToken cancellationToken)
    {
        var accessToken = await connection.GetAccessTokenAsync(settings, cancellationToken);
        var nextUrl = string.IsNullOrWhiteSpace(settings.MicrosoftDeltaLink) ? InitialDeltaUrl : settings.MicrosoftDeltaLink;
        var imported = 0;
        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using var document = await GetJsonAsync(nextUrl, accessToken, cancellationToken);
            foreach (var item in document.RootElement.GetProperty("value").EnumerateArray()) imported += await UpsertMessageAsync(settings, item, accessToken, cancellationToken);
            if (document.RootElement.TryGetProperty("@odata.nextLink", out var next)) nextUrl = next.GetString();
            else { settings.MicrosoftDeltaLink = document.RootElement.TryGetProperty("@odata.deltaLink", out var delta) ? delta.GetString() : null; nextUrl = null; }
        }
        await db.SaveChangesAsync(cancellationToken);
        return imported;
    }

    public async Task<EmailMessageDto> SendAsync(EmailAccountSettings settings, SendEmailRequest request, CancellationToken cancellationToken)
    {
        var accessToken = await connection.GetAccessTokenAsync(settings, cancellationToken);
        var replyTo = request.ReplyToEmailId.HasValue ? await db.EmailMessages.SingleOrDefaultAsync(x => x.Id == request.ReplyToEmailId && x.UserAccountId == settings.UserAccountId, cancellationToken) : null;
        if (request.ReplyToEmailId.HasValue && replyTo is null) throw new DomainValidationException("Původní e-mail nebyl nalezen.");
        var isGraphReply = replyTo is not null && !replyTo.ExternalId.StartsWith("sent:", StringComparison.Ordinal);
        if (isGraphReply)
        {
            var replyMessage = new { body = new { contentType = "HTML", content = request.BodyHtml }, toRecipients = Recipients(request.ToAddress), ccRecipients = Recipients(request.CcAddress) };
            await SendJsonAsync(HttpMethod.Post, $"https://graph.microsoft.com/v1.0/me/messages/{Uri.EscapeDataString(replyTo!.ExternalId)}/reply", new { message = replyMessage }, accessToken, cancellationToken);
        }
        else
        {
            var message = new { subject = request.Subject.Trim(), body = new { contentType = "HTML", content = request.BodyHtml }, toRecipients = Recipients(request.ToAddress), ccRecipients = Recipients(request.CcAddress) };
            await SendJsonAsync(HttpMethod.Post, "https://graph.microsoft.com/v1.0/me/sendMail", new { message, saveToSentItems = true }, accessToken, cancellationToken);
        }
        var mailbox = settings.MicrosoftMailboxAddress ?? settings.SenderAddress;
        var sent = new EmailMessage { UserAccountId = settings.UserAccountId, Direction = EmailDirection.Outbound, ExternalId = $"sent:{Guid.NewGuid():N}", Mailbox = "Sent Items", FromAddress = mailbox, FromName = settings.SenderName, ToAddress = request.ToAddress.Trim(), CcAddress = string.IsNullOrWhiteSpace(request.CcAddress) ? null : request.CcAddress.Trim(), Subject = request.Subject.Trim(), BodyHtml = request.BodyHtml, OccurredAtUtc = DateTime.UtcNow, IsRead = true, BusinessPartnerId = request.BusinessPartnerId ?? replyTo?.BusinessPartnerId, OrderId = request.OrderId ?? replyTo?.OrderId };
        db.EmailMessages.Add(sent);
        await db.SaveChangesAsync(cancellationToken);
        return EmailQueryService.Map(sent);
    }

    private async Task<int> UpsertMessageAsync(EmailAccountSettings settings, JsonElement item, string accessToken, CancellationToken cancellationToken)
    {
        if (item.TryGetProperty("@removed", out _)) return 0;
        var externalId = item.GetProperty("id").GetString()!;
        var email = await db.EmailMessages.SingleOrDefaultAsync(x => x.UserAccountId == settings.UserAccountId && x.ExternalId == externalId, cancellationToken);
        var isNew = email is null;
        email ??= new EmailMessage { UserAccountId = settings.UserAccountId, Direction = EmailDirection.Inbound, ExternalId = externalId, Mailbox = "Inbox" };
        var from = TryGetAddress(item, "from");
        email.FromAddress = from.Address;
        email.FromName = from.Name;
        email.ToAddress = JoinRecipients(item, "toRecipients");
        email.CcAddress = JoinRecipients(item, "ccRecipients");
        email.Subject = item.TryGetProperty("subject", out var subject) ? subject.GetString() ?? "(bez předmětu)" : "(bez předmětu)";
        if (item.TryGetProperty("body", out var body)) { var content = body.TryGetProperty("content", out var bodyContent) ? bodyContent.GetString() : null; var contentType = body.TryGetProperty("contentType", out var type) ? type.GetString() : null; email.BodyHtml = string.Equals(contentType, "html", StringComparison.OrdinalIgnoreCase) ? content : null; email.BodyText = string.Equals(contentType, "text", StringComparison.OrdinalIgnoreCase) ? content : null; }
        email.OccurredAtUtc = item.TryGetProperty("receivedDateTime", out var received) && DateTimeOffset.TryParse(received.GetString(), out var occurred) ? occurred.UtcDateTime : DateTime.UtcNow;
        email.IsRead = item.TryGetProperty("isRead", out var isRead) && isRead.GetBoolean();
        email.HasAttachments = item.TryGetProperty("hasAttachments", out var hasAttachments) && hasAttachments.GetBoolean();
        if (!isNew) return 0;
        await emailProcessor.ProcessAsync(email, cancellationToken);
        db.EmailMessages.Add(email);
        if (email.HasAttachments) await ImportAttachmentsAsync(email, externalId, accessToken, cancellationToken);
        return 1;
    }

    private async Task ImportAttachmentsAsync(EmailMessage email, string messageId, string accessToken, CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync($"https://graph.microsoft.com/v1.0/me/messages/{Uri.EscapeDataString(messageId)}/attachments?$select=name,contentType,size,contentBytes,isInline", accessToken, cancellationToken);
        foreach (var attachment in document.RootElement.GetProperty("value").EnumerateArray())
        {
            if (attachment.TryGetProperty("isInline", out var inline) && inline.GetBoolean()) continue;
            if (attachment.TryGetProperty("size", out var declaredSize) && declaredSize.GetInt64() > 2 * 1024 * 1024) continue;
            if (!attachment.TryGetProperty("contentBytes", out var contentBytes) || string.IsNullOrWhiteSpace(contentBytes.GetString())) continue;
            var encodedContent = contentBytes.GetString()!;
            if (encodedContent.Length > 2_796_208) continue;
            var bytes = Convert.FromBase64String(encodedContent);
            if (bytes.Length > 2 * 1024 * 1024) continue;
            var fileName = attachment.TryGetProperty("name", out var name) ? name.GetString() ?? "priloha.bin" : "priloha.bin";
            var contentType = attachment.TryGetProperty("contentType", out var type) ? type.GetString() ?? "application/octet-stream" : "application/octet-stream";
            await using var stream = new MemoryStream(bytes, writable: false);
            var key = await fileStorage.SaveAsync(fileName, stream, cancellationToken);
            db.Attachments.Add(new Attachment { EmailMessageId = email.Id, FileName = fileName, ContentType = contentType, Size = bytes.Length, StorageKey = key });
        }
    }

    private async Task<JsonDocument> GetJsonAsync(string url, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("Prefer", "outlook.body-content-type=\"html\"");
        using var response = await httpClientFactory.CreateClient("MicrosoftGraph").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private async Task SendJsonAsync(HttpMethod method, string url, object value, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("MicrosoftGraph").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static object[] Recipients(string? addresses) => string.IsNullOrWhiteSpace(addresses) ? [] : addresses.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => new { emailAddress = new { address = x } }).Cast<object>().ToArray();
    private static string JoinRecipients(JsonElement item, string property) => item.TryGetProperty(property, out var recipients) ? string.Join(", ", recipients.EnumerateArray().Select(x => x.GetProperty("emailAddress").GetProperty("address").GetString()).Where(x => !string.IsNullOrWhiteSpace(x))) : "";
    private static (string Address, string? Name) TryGetAddress(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var recipient) || recipient.ValueKind == JsonValueKind.Null || !recipient.TryGetProperty("emailAddress", out var address)) return ("", null);
        return (address.TryGetProperty("address", out var value) ? value.GetString() ?? "" : "", address.TryGetProperty("name", out var name) ? name.GetString() : null);
    }
}
