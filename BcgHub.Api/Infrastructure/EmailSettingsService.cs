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
        return settings is null ? null : new(settings.ImapServer, settings.ImapPort, settings.ImapUseSsl, settings.ImapUsername, !string.IsNullOrEmpty(settings.ProtectedImapPassword), settings.IsActive);
    }

    public async Task<EmailSettingsDto> SaveAsync(SaveEmailSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!request.ImapUseSsl) throw new DomainValidationException("Připojení k e-mailové schránce musí používat TLS.");
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken);
        settings ??= new EmailAccountSettings { UserAccountId = currentUser.UserId };
        if (db.Entry(settings).State == EntityState.Detached) db.EmailAccountSettings.Add(settings);
        settings.ImapServer = request.ImapServer.Trim();
        settings.ImapPort = request.ImapPort;
        settings.ImapUseSsl = true;
        settings.ImapUsername = request.ImapUsername.Trim();
        settings.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.ImapPassword)) settings.ProtectedImapPassword = _protector.Protect(request.ImapPassword);
        if (string.IsNullOrEmpty(settings.ProtectedImapPassword)) throw new DomainValidationException("Heslo k e-mailové schránce je povinné.");
        await db.SaveChangesAsync(cancellationToken);
        return new(settings.ImapServer, settings.ImapPort, true, settings.ImapUsername, true, settings.IsActive);
    }

    public string Unprotect(EmailAccountSettings settings) => _protector.Unprotect(settings.ProtectedImapPassword);
}
