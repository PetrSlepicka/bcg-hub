using System.Text.Json;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BcgHub.Api.Infrastructure;

public sealed class MicrosoftGraphConnectionService(BcgHubDbContext db, CurrentUserAccessor currentUser, IDataProtectionProvider protectionProvider, IHttpClientFactory httpClientFactory, IOptions<MicrosoftGraphOptions> options, IConfiguration configuration) : IMicrosoftGraphConnectionService
{
    private const string Scopes = "openid profile email offline_access User.Read Mail.Read Mail.Send";
    private readonly IDataProtector _tokenProtector = protectionProvider.CreateProtector("BcgHub.MicrosoftGraph.RefreshToken.v1");
    private readonly IDataProtector _stateProtector = protectionProvider.CreateProtector("BcgHub.MicrosoftGraph.OAuthState.v1");
    private MicrosoftGraphOptions Options => options.Value;

    public string CreateAuthorizationUrl(string redirectUri, string returnUrl)
    {
        EnsureConfigured();
        var safeReturnUrl = ValidateReturnUrl(returnUrl);
        var state = _stateProtector.Protect(JsonSerializer.Serialize(new OAuthState(currentUser.UserId, safeReturnUrl, DateTime.UtcNow.AddMinutes(10))));
        return $"https://login.microsoftonline.com/{Uri.EscapeDataString(Options.TenantId)}/oauth2/v2.0/authorize?client_id={Uri.EscapeDataString(Options.ClientId)}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_mode=query&scope={Uri.EscapeDataString(Scopes)}&state={Uri.EscapeDataString(state)}&prompt=select_account";
    }

    public async Task<string> CompleteAuthorizationAsync(string code, string state, string redirectUri, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var payload = JsonSerializer.Deserialize<OAuthState>(_stateProtector.Unprotect(state)) ?? throw new DomainValidationException("Připojení Microsoft účtu má neplatný stav.");
        if (payload.ExpiresAtUtc < DateTime.UtcNow) throw new DomainValidationException("Připojení Microsoft účtu vypršelo. Spusťte je znovu.");
        if (payload.UserId != currentUser.UserId) throw new UnauthorizedAccessException();
        var token = await RequestTokenAsync(new Dictionary<string, string> { ["client_id"] = Options.ClientId, ["client_secret"] = Options.ClientSecret, ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = redirectUri, ["scope"] = Scopes }, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.RefreshToken)) throw new DomainValidationException("Microsoft nevrátil oprávnění pro dlouhodobé připojení schránky.");
        var mailbox = await GetMailboxAddressAsync(token.AccessToken, cancellationToken);
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken) ?? new EmailAccountSettings { UserAccountId = currentUser.UserId };
        if (db.Entry(settings).State == EntityState.Detached) db.EmailAccountSettings.Add(settings);
        settings.Provider = EmailProvider.MicrosoftGraph;
        settings.ProtectedMicrosoftRefreshToken = _tokenProtector.Protect(token.RefreshToken);
        settings.MicrosoftMailboxAddress = mailbox;
        settings.MicrosoftDeltaLink = null;
        settings.SenderAddress = mailbox;
        settings.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
        return AppendStatus(payload.ReturnUrl, "microsoft-connected");
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        var settings = await db.EmailAccountSettings.SingleOrDefaultAsync(x => x.UserAccountId == currentUser.UserId, cancellationToken);
        if (settings is null) return;
        settings.ProtectedMicrosoftRefreshToken = null;
        settings.MicrosoftMailboxAddress = null;
        settings.MicrosoftDeltaLink = null;
        if (settings.Provider == EmailProvider.MicrosoftGraph) { settings.Provider = EmailProvider.ImapSmtp; settings.IsActive = !string.IsNullOrEmpty(settings.ProtectedImapPassword) && !string.IsNullOrEmpty(settings.ProtectedSmtpPassword); }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetAccessTokenAsync(EmailAccountSettings settings, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(settings.ProtectedMicrosoftRefreshToken)) throw new DomainValidationException("Microsoft účet není připojen.");
        var refreshToken = _tokenProtector.Unprotect(settings.ProtectedMicrosoftRefreshToken);
        var token = await RequestTokenAsync(new Dictionary<string, string> { ["client_id"] = Options.ClientId, ["client_secret"] = Options.ClientSecret, ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken, ["scope"] = Scopes }, cancellationToken);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken) && token.RefreshToken != refreshToken) { settings.ProtectedMicrosoftRefreshToken = _tokenProtector.Protect(token.RefreshToken); await db.SaveChangesAsync(cancellationToken); }
        return token.AccessToken;
    }

    private async Task<TokenResponse> RequestTokenAsync(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        using var response = await httpClientFactory.CreateClient("MicrosoftGraph").PostAsync($"https://login.microsoftonline.com/{Uri.EscapeDataString(Options.TenantId)}/oauth2/v2.0/token", new FormUrlEncodedContent(values), cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new DomainValidationException("Microsoft autorizaci se nepodařilo dokončit.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        return new TokenResponse(root.GetProperty("access_token").GetString()!, root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null);
    }

    private async Task<string> GetMailboxAddressAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=mail,userPrincipalName");
        request.Headers.Authorization = new("Bearer", accessToken);
        using var response = await httpClientFactory.CreateClient("MicrosoftGraph").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.TryGetProperty("mail", out var mail) && !string.IsNullOrWhiteSpace(mail.GetString()) ? mail.GetString()! : json.RootElement.GetProperty("userPrincipalName").GetString()!;
    }

    private string ValidateReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)) throw new DomainValidationException("Návratová adresa aplikace je neplatná.");
        var origin = uri.GetLeftPart(UriPartial.Authority);
        var allowed = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (!allowed.Any(x => string.Equals(x.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))) throw new DomainValidationException("Návratová adresa aplikace není povolená.");
        return returnUrl;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(Options.ClientId) || string.IsNullOrWhiteSpace(Options.ClientSecret)) throw new DomainValidationException("Microsoft Graph není nakonfigurován správcem aplikace.");
    }

    private static string AppendStatus(string returnUrl, string status) => $"{returnUrl}{(returnUrl.Contains('?') ? '&' : '?')}emailConnection={Uri.EscapeDataString(status)}";
    private sealed record OAuthState(Guid UserId, string ReturnUrl, DateTime ExpiresAtUtc);
    private sealed record TokenResponse(string AccessToken, string? RefreshToken);
}
