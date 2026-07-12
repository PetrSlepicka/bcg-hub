using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class TransportInquiryService(BcgHubDbContext db, IEmailSender emailSender) : ITransportInquiryService
{
    private static readonly IReadOnlyDictionary<string, string[]> CapabilityTerms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Road"] = ["road", "pozem", "silni"],
        ["Sea"] = ["sea", "námoř", "namor"],
        ["Air"] = ["air", "letec"]
    };

    public async Task<TransportInquiryContextDto?> GetContextAsync(Guid orderId, string transportType, CancellationToken cancellationToken)
    {
        var orderNumber = await db.Orders.AsNoTracking().Where(x => x.Id == orderId).Select(x => x.Number).SingleOrDefaultAsync(cancellationToken);
        if (orderNumber is null) return null;
        var terms = GetTerms(transportType);
        var carriers = await db.BusinessPartners.AsNoTracking().Where(x => x.Type == PartnerType.Carrier && x.TransportCapabilities != null).Include(x => x.Contacts).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return new TransportInquiryContextDto(orderNumber, carriers.Where(x => Supports(x, terms)).Select(ToDto).Where(x => x is not null).Cast<TransportInquiryCarrierDto>().ToList());
    }

    public async Task<SendTransportInquiryResult?> SendAsync(Guid orderId, SendTransportInquiryRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null) return null;
        var terms = GetTerms(request.TransportType);
        if (request.CarrierIds.Count == 0) throw new DomainValidationException("Vyberte alespoň jednoho dopravce.");
        var selectedIds = request.CarrierIds.Distinct().ToList();
        var carriers = await db.BusinessPartners.AsNoTracking().Where(x => x.Type == PartnerType.Carrier && selectedIds.Contains(x.Id)).Include(x => x.Contacts).ToListAsync(cancellationToken);
        if (carriers.Count != selectedIds.Count) throw new DomainValidationException("Některý z vybraných dopravců neexistuje.");
        if (carriers.Any(x => !Supports(x, terms))) throw new DomainValidationException("Některý z vybraných dopravců nenabízí zvolený typ dopravy.");
        var recipients = carriers.Select(x => new { Carrier = x, Email = PreferredEmail(x) }).ToList();
        if (recipients.Any(x => x.Email is null)) throw new DomainValidationException("Některý z vybraných dopravců nemá e-mailovou adresu.");
        var subject = EnsureOrderNumber(request.Subject, order.Number);
        foreach (var recipient in recipients) await emailSender.SendAsync(new SendEmailRequest(recipient.Email!, null, subject, request.BodyHtml, null, recipient.Carrier.Id, order.Id), cancellationToken);
        return new SendTransportInquiryResult(recipients.Count);
    }

    internal static string EnsureOrderNumber(string subject, string orderNumber)
    {
        var cleanSubject = subject.Trim();
        return cleanSubject.Contains(orderNumber, StringComparison.OrdinalIgnoreCase) ? cleanSubject : $"[{orderNumber}] {cleanSubject}";
    }

    private static string[] GetTerms(string transportType) => CapabilityTerms.TryGetValue(transportType, out var terms) ? terms : throw new DomainValidationException("Neplatný typ dopravy.");
    private static bool Supports(BusinessPartner carrier, IEnumerable<string> terms) => carrier.TransportCapabilities is { } capabilities && terms.Any(term => capabilities.Contains(term, StringComparison.OrdinalIgnoreCase));
    private static TransportInquiryCarrierDto? ToDto(BusinessPartner carrier) => PreferredEmail(carrier) is { } email ? new TransportInquiryCarrierDto(carrier.Id, carrier.Name, email) : null;
    private static string? PreferredEmail(BusinessPartner carrier) => carrier.Contacts.Where(x => x.IsPrimary && !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email).FirstOrDefault() ?? carrier.Email;
}
