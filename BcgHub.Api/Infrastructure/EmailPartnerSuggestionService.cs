using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailPartnerSuggestionService(BcgHubDbContext db, IEmailSenderResolver senderResolver) : IEmailPartnerSuggestionService
{
    public async Task<EmailPartnerSuggestionResult> ResolveAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var match = await senderResolver.ResolveAsync(email.FromAddress, cancellationToken);
        var linked = email.BusinessPartnerId.HasValue ? await db.BusinessPartners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == email.BusinessPartnerId, cancellationToken) : null;
        var previous = linked is null ? await FindPreviousPartnerAsync(email, cancellationToken) : null;
        return Create(match, linked, previous);
    }

    internal static EmailPartnerSuggestionResult Create(EmailSenderMatch match, BusinessPartner? linked, BusinessPartner? previous)
    {
        var candidates = new List<BusinessPartner>();
        Add(candidates, linked);
        if (match.Kind == EmailSenderMatchKind.Address) AddRange(candidates, match.Partners);
        Add(candidates, previous);
        if (match.Kind == EmailSenderMatchKind.Domain) AddRange(candidates, match.Partners);

        if (linked is not null) return new EmailPartnerSuggestionResult(linked, "Manual", candidates);
        if (match.IsAmbiguous) return new EmailPartnerSuggestionResult(null, "Ambiguous", candidates);
        if (match.Kind == EmailSenderMatchKind.Address && match.Partner is not null) return new EmailPartnerSuggestionResult(match.Partner, "Address", candidates);
        if (previous is not null) return new EmailPartnerSuggestionResult(previous, "History", candidates);
        if (match.Kind == EmailSenderMatchKind.Domain && match.Partner is not null) return new EmailPartnerSuggestionResult(match.Partner, "Domain", candidates);
        return new EmailPartnerSuggestionResult(null, "None", candidates);
    }

    private async Task<BusinessPartner?> FindPreviousPartnerAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email.FromAddress)) return null;
        var normalized = email.FromAddress.Trim().ToLowerInvariant();
        var partnerId = await db.EmailMessages.AsNoTracking().Where(x => x.Id != email.Id && x.UserAccountId == email.UserAccountId && x.FromAddress.ToLower() == normalized && x.BusinessPartnerId.HasValue).OrderByDescending(x => x.OccurredAtUtc).Select(x => x.BusinessPartnerId).FirstOrDefaultAsync(cancellationToken);
        return partnerId.HasValue ? await db.BusinessPartners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == partnerId.Value, cancellationToken) : null;
    }

    private static void Add(ICollection<BusinessPartner> candidates, BusinessPartner? partner) { if (partner is not null && candidates.All(x => x.Id != partner.Id)) candidates.Add(partner); }
    private static void AddRange(ICollection<BusinessPartner> candidates, IEnumerable<BusinessPartner> partners) { foreach (var partner in partners) Add(candidates, partner); }
}
