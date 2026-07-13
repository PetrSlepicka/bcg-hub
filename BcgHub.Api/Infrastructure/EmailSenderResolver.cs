using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailSenderResolver(BcgHubDbContext db) : IEmailSenderResolver
{
    private static readonly HashSet<string> PublicDomains = new(StringComparer.OrdinalIgnoreCase) { "centrum.cz", "email.cz", "gmail.com", "hotmail.com", "icloud.com", "outlook.com", "proton.me", "protonmail.com", "seznam.cz", "yahoo.com" };

    public async Task<EmailSenderMatch> ResolveAsync(string address, CancellationToken cancellationToken)
    {
        var normalizedAddress = NormalizeAddress(address);
        if (normalizedAddress is null) return new EmailSenderMatch([], EmailSenderMatchKind.None);
        var exactMatches = await db.BusinessPartners.AsNoTracking().Where(partner => partner.Email != null && partner.Email.ToLower() == normalizedAddress || partner.Contacts.Any(contact => contact.Email != null && contact.Email.ToLower() == normalizedAddress)).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        if (exactMatches.Count > 0) return new EmailSenderMatch(exactMatches, EmailSenderMatchKind.Address);
        var domain = GetDomain(normalizedAddress);
        if (domain is null || PublicDomains.Contains(domain)) return new EmailSenderMatch([], EmailSenderMatchKind.None);
        var suffix = $"@{domain}";
        var domainMatches = await db.BusinessPartners.AsNoTracking().Where(partner => partner.Email != null && partner.Email.ToLower().EndsWith(suffix) || partner.Contacts.Any(contact => contact.Email != null && contact.Email.ToLower().EndsWith(suffix))).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return new EmailSenderMatch(domainMatches, domainMatches.Count == 0 ? EmailSenderMatchKind.None : EmailSenderMatchKind.Domain);
    }

    internal static EmailSenderMatch Resolve(IReadOnlyCollection<BusinessPartner> partners, string address)
    {
        var normalizedAddress = NormalizeAddress(address);
        if (normalizedAddress is null) return new EmailSenderMatch([], EmailSenderMatchKind.None);
        var exactMatches = partners.Where(partner => NormalizeAddress(partner.Email) == normalizedAddress || partner.Contacts.Any(contact => NormalizeAddress(contact.Email) == normalizedAddress)).ToList();
        if (exactMatches.Count > 0) return new EmailSenderMatch(exactMatches, EmailSenderMatchKind.Address);
        var domain = GetDomain(normalizedAddress);
        if (domain is null || PublicDomains.Contains(domain)) return new EmailSenderMatch([], EmailSenderMatchKind.None);
        var domainMatches = partners.Where(partner => GetDomain(partner.Email) == domain || partner.Contacts.Any(contact => GetDomain(contact.Email) == domain)).ToList();
        return new EmailSenderMatch(domainMatches, domainMatches.Count == 0 ? EmailSenderMatchKind.None : EmailSenderMatchKind.Domain);
    }

    internal static string? GetDomain(string? address)
    {
        var normalized = NormalizeAddress(address);
        if (normalized is null) return null;
        var separator = normalized.LastIndexOf('@');
        return separator > 0 && separator < normalized.Length - 1 ? normalized[(separator + 1)..] : null;
    }

    private static string? NormalizeAddress(string? address) => string.IsNullOrWhiteSpace(address) ? null : address.Trim().ToLowerInvariant();
}
