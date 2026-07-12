using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSettingsService(BcgHubDbContext db, CurrentUserAccessor currentUser, IDataProtectionProvider protectionProvider) : IEmailSettingsService
{
    private readonly IDataProtector _protector = protectionProvider.CreateProtector("BcgHub.EmailCredentials.v1");

    public async Task<EmailSettingsDto?> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await db.EmailAccountSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken);
        return settings is null ? null : Map(settings);
    }

    public async Task<EmailSettingsDto> SaveAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!request.ImapUseSsl || !request.SmtpUseSsl) throw new DomainValidationException("Připojení k e-mailové schránce musí používat TLS.");
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken);
        settings ??= new EmailAccountSettings { UserAccountId = currentUser.UserId };
        if (db.Entry(settings).State == EntityState.Detached) db.EmailAccountSettings.Add(settings);
        settings.ImapServer = request.ImapServer.Trim();
        settings.Provider = EmailProvider.ImapSmtp;
        settings.ImapPort = request.ImapPort;
        settings.ImapUseSsl = true;
        settings.ImapUsername = request.ImapUsername.Trim();
        settings.SmtpServer = request.SmtpServer.Trim();
        settings.SmtpPort = request.SmtpPort;
        settings.SmtpUseSsl = true;
        settings.SmtpUsername = request.SmtpUsername.Trim();
        settings.SenderAddress = request.SenderAddress.Trim();
        settings.SenderName = string.IsNullOrWhiteSpace(request.SenderName) ? null : request.SenderName.Trim();
        settings.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.ImapPassword)) settings.ProtectedImapPassword = _protector.Protect(request.ImapPassword);
        if (!string.IsNullOrWhiteSpace(request.SmtpPassword)) settings.ProtectedSmtpPassword = _protector.Protect(request.SmtpPassword);
        if (string.IsNullOrEmpty(settings.ProtectedImapPassword)) throw new DomainValidationException("Heslo k e-mailové schránce je povinné.");
        if (string.IsNullOrEmpty(settings.ProtectedSmtpPassword)) throw new DomainValidationException("Heslo pro odesílání e-mailů je povinné.");
        await db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public async Task<EmailSettingsDto> SetProviderAsync(string provider, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EmailProvider>(provider, true, out var parsed)) throw new DomainValidationException("Neznámý poskytovatel e-mailu.");
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken) ?? new EmailAccountSettings { UserAccountId = currentUser.UserId };
        if (parsed == EmailProvider.MicrosoftGraph && string.IsNullOrEmpty(settings.ProtectedMicrosoftRefreshToken)) throw new DomainValidationException("Nejdříve připojte účet Microsoft.");
        if (db.Entry(settings).State == EntityState.Detached) db.EmailAccountSettings.Add(settings);
        settings.Provider = parsed;
        settings.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return Map(settings);
    }

    public string Unprotect(EmailAccountSettings settings) => _protector.Unprotect(settings.ProtectedImapPassword);
    public string UnprotectSmtpPassword(EmailAccountSettings settings) => _protector.Unprotect(settings.ProtectedSmtpPassword);

    internal static EmailSettingsDto Map(EmailAccountSettings settings) => new(settings.Provider.ToString(), settings.ImapServer, settings.ImapPort, settings.ImapUseSsl, settings.ImapUsername, !string.IsNullOrEmpty(settings.ProtectedImapPassword), settings.SmtpServer, settings.SmtpPort, settings.SmtpUseSsl, settings.SmtpUsername, !string.IsNullOrEmpty(settings.ProtectedSmtpPassword), settings.SenderAddress, settings.SenderName, settings.IsActive, !string.IsNullOrEmpty(settings.ProtectedMicrosoftRefreshToken), settings.MicrosoftMailboxAddress);
}
