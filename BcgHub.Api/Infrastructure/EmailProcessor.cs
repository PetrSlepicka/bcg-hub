using System.Text.RegularExpressions;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed partial class EmailProcessor(BcgHubDbContext db) : IEmailProcessor
{
    [GeneratedRegex(@"(?<![A-Z0-9_])BCG_\d{8}(?![A-Z0-9_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberRegex();

    public async Task ProcessAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var previousLink = await FindPreviousLinkAsync(email.UserAccountId, email.FromAddress, cancellationToken);
        email.BusinessPartnerId = await FindPartnerIdAsync(email.FromAddress, cancellationToken) ?? previousLink.BusinessPartnerId;
        var number = ExtractOrderNumber(email.Subject);
        email.OrderId = number is null ? previousLink.OrderId : await db.Orders.AsNoTracking().Where(x => x.Number == number).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (email.OrderId.HasValue) email.BusinessPartnerId = await db.Orders.AsNoTracking().Where(x => x.Id == email.OrderId).Select(x => (Guid?)x.CustomerId).SingleAsync(cancellationToken);
    }

    public async Task<EmailOrderOptionsDto> GetOrderOptionsAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var partnerId = email.BusinessPartnerId ?? await FindPartnerIdAsync(email.FromAddress, cancellationToken);
        var orders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { Option = new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name), x.CustomerId }).ToListAsync(cancellationToken);
        return new EmailOrderOptionsDto(orders.Where(x => partnerId.HasValue && x.CustomerId == partnerId).Select(x => x.Option).ToList(), orders.Where(x => !partnerId.HasValue || x.CustomerId != partnerId).Select(x => x.Option).ToList());
    }

    public async Task<EmailActionContextDto> GetActionContextAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        if (email.Direction != EmailDirection.Inbound) return new EmailActionContextDto("Unknown", "None", null);
        var partners = await db.BusinessPartners.AsNoTracking().Include(x => x.Contacts).ToListAsync(cancellationToken);
        var normalizedAddress = NormalizeAddress(email.FromAddress);
        var exact = partners.FirstOrDefault(partner => NormalizeAddress(partner.Email) == normalizedAddress || partner.Contacts.Any(contact => NormalizeAddress(contact.Email) == normalizedAddress));
        if (exact is not null) return ToActionContext(exact, "Address");
        var domain = EmailTransportQuoteService.GetDomain(email.FromAddress);
        var domainMatch = domain is null ? null : partners.FirstOrDefault(partner => EmailTransportQuoteService.GetDomain(partner.Email) == domain || partner.Contacts.Any(contact => EmailTransportQuoteService.GetDomain(contact.Email) == domain));
        return domainMatch is null ? new EmailActionContextDto("Unknown", "None", null) : ToActionContext(domainMatch, "Domain");
    }

    internal static string? ExtractOrderNumber(string? subject)
    {
        var match = OrderNumberRegex().Match(subject ?? "");
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private async Task<Guid?> FindPartnerIdAsync(string? address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var normalized = address.Trim().ToLower();
        var partnerId = await db.BusinessPartners.AsNoTracking().Where(x => x.Email != null && x.Email.ToLower() == normalized).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return partnerId ?? await db.ContactPeople.AsNoTracking().Where(x => x.Email != null && x.Email.ToLower() == normalized).Select(x => (Guid?)x.BusinessPartnerId).FirstOrDefaultAsync(cancellationToken);
    }

    private static EmailActionContextDto ToActionContext(BusinessPartner partner, string matchedBy) => new(SenderType(partner.Type), matchedBy, new PartnerReference(partner.Id, partner.Name));

    private static string SenderType(PartnerType type) => type switch { PartnerType.Carrier => "Carrier", PartnerType.Warehouse => "Warehouse", PartnerType.Collaborator => "Collaborator", PartnerType.Customer => "Customer", _ => "Partner" };

    private static string? NormalizeAddress(string? address) => string.IsNullOrWhiteSpace(address) ? null : address.Trim().ToLowerInvariant();

    private async Task<(Guid? BusinessPartnerId, Guid? OrderId)> FindPreviousLinkAsync(Guid userAccountId, string? address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address)) return (null, null);
        var normalized = address.Trim().ToLower();
        var link = await db.EmailMessages.AsNoTracking().Where(x => x.UserAccountId == userAccountId && x.FromAddress.ToLower() == normalized && (x.BusinessPartnerId.HasValue || x.OrderId.HasValue)).OrderByDescending(x => x.OccurredAtUtc).Select(x => new { x.BusinessPartnerId, x.OrderId }).FirstOrDefaultAsync(cancellationToken);
        return link is null ? (null, null) : (link.BusinessPartnerId, link.OrderId);
    }
}
