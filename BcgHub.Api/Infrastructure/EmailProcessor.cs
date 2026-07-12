using System.Text.RegularExpressions;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed partial class EmailProcessor(BcgHubDbContext db, IEmailSenderResolver senderResolver) : IEmailProcessor
{
    [GeneratedRegex(@"(?<![A-Z0-9_])BCG_\d{8}(?![A-Z0-9_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberRegex();

    public async Task ProcessAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var previousPartnerId = await FindPreviousPartnerIdAsync(email.UserAccountId, email.FromAddress, cancellationToken);
        var sender = await senderResolver.ResolveAsync(email.FromAddress, cancellationToken);
        email.BusinessPartnerId = sender.Kind == EmailSenderMatchKind.Address ? sender.Partner?.Id : previousPartnerId;
        var number = ExtractOrderNumber(email.Subject);
        email.OrderId = number is null ? null : await db.Orders.AsNoTracking().Where(x => x.Number == number).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailOrderOptionsDto> GetOrderOptionsAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var sender = await senderResolver.ResolveAsync(email.FromAddress, cancellationToken);
        var partner = email.BusinessPartnerId.HasValue ? await db.BusinessPartners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == email.BusinessPartnerId, cancellationToken) : sender.Partner;
        var orderNumber = ExtractOrderNumber(email.Subject);
        var orders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { Option = new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name), x.CustomerId, x.WarehouseId, x.CarrierId }).ToListAsync(cancellationToken);
        var suggested = orders.Where(x => x.Option.Id == email.OrderId || x.Option.Number == orderNumber || partner != null && IsRelatedOrder(partner, x.CustomerId, x.WarehouseId, x.CarrierId)).Select(x => x.Option).DistinctBy(x => x.Id).ToList();
        var suggestedIds = suggested.Select(x => x.Id).ToHashSet();
        return new EmailOrderOptionsDto(suggested, orders.Where(x => !suggestedIds.Contains(x.Option.Id)).Select(x => x.Option).ToList());
    }

    public async Task<EmailActionContextDto> GetActionContextAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        if (email.Direction != EmailDirection.Inbound) return new EmailActionContextDto("Unknown", "None", null);
        var match = await senderResolver.ResolveAsync(email.FromAddress, cancellationToken);
        var manuallyLinkedPartner = email.BusinessPartnerId.HasValue ? await db.BusinessPartners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == email.BusinessPartnerId, cancellationToken) : null;
        if (manuallyLinkedPartner is not null && manuallyLinkedPartner.Id != match.Partner?.Id) return ToActionContext(manuallyLinkedPartner, "Manual");
        if (match.Partner is not null) return ToActionContext(match.Partner, match.Kind.ToString());
        return manuallyLinkedPartner is null ? new EmailActionContextDto("Unknown", match.Kind.ToString(), null) : ToActionContext(manuallyLinkedPartner, "Manual");
    }

    internal static string? ExtractOrderNumber(string? subject)
    {
        var match = OrderNumberRegex().Match(subject ?? "");
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static EmailActionContextDto ToActionContext(BusinessPartner partner, string matchedBy) => new(SenderType(partner.Type), matchedBy, new PartnerReference(partner.Id, partner.Name));

    private static string SenderType(PartnerType type) => type switch { PartnerType.Carrier => "Carrier", PartnerType.Warehouse => "Warehouse", PartnerType.Collaborator => "Collaborator", PartnerType.Customer => "Customer", _ => "Partner" };

    private static bool IsRelatedOrder(BusinessPartner partner, Guid customerId, Guid? warehouseId, Guid? carrierId) => partner.Type switch { PartnerType.Customer => customerId == partner.Id, PartnerType.Warehouse => warehouseId == partner.Id, PartnerType.Carrier => carrierId == partner.Id, _ => false };

    private async Task<Guid?> FindPreviousPartnerIdAsync(Guid userAccountId, string? address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var normalized = address.Trim().ToLower();
        return await db.EmailMessages.AsNoTracking().Where(x => x.UserAccountId == userAccountId && x.FromAddress.ToLower() == normalized && x.BusinessPartnerId.HasValue).OrderByDescending(x => x.OccurredAtUtc).Select(x => x.BusinessPartnerId).FirstOrDefaultAsync(cancellationToken);
    }
}
