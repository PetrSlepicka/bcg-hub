using System.Text.RegularExpressions;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed partial class EmailProcessor(BcgHubDbContext db, IEmailPartnerSuggestionService partnerSuggestions) : IEmailProcessor
{
    [GeneratedRegex(@"(?<![A-Z0-9_])BCG_\d{8}(?![A-Z0-9_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderNumberRegex();

    public async Task ProcessAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var number = ExtractOrderNumber(email.Subject);
        email.OrderId = number is null ? null : await db.Orders.AsNoTracking().Where(x => x.Number == number).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailOrderOptionsDto> GetOrderOptionsAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var partner = (await partnerSuggestions.ResolveAsync(email, cancellationToken)).PreferredPartner;
        var orderNumber = ExtractOrderNumber(email.Subject);
        var orders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { Option = new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name), x.CustomerId, x.WarehouseId, x.CarrierId }).ToListAsync(cancellationToken);
        var suggested = orders.Where(x => x.Option.Id == email.OrderId || x.Option.Number == orderNumber || partner != null && IsRelatedOrder(partner, x.CustomerId, x.WarehouseId, x.CarrierId)).Select(x => x.Option).DistinctBy(x => x.Id).ToList();
        var suggestedIds = suggested.Select(x => x.Id).ToHashSet();
        return new EmailOrderOptionsDto(suggested, orders.Where(x => !suggestedIds.Contains(x.Option.Id)).Select(x => x.Option).ToList());
    }

    public async Task<EmailActionContextDto> GetActionContextAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        if (email.Direction != EmailDirection.Inbound) return new EmailActionContextDto("Unknown", "None", null, []);
        var result = await partnerSuggestions.ResolveAsync(email, cancellationToken);
        var suggestions = result.Candidates.Select(x => new EmailPartnerSuggestionDto(x.Id, x.Name, x.Type.ToString(), x.Email)).ToList();
        return result.PreferredPartner is null ? new EmailActionContextDto("Unknown", result.MatchedBy, null, suggestions) : ToActionContext(result.PreferredPartner, result.MatchedBy, suggestions);
    }

    internal static string? ExtractOrderNumber(string? subject)
    {
        var match = OrderNumberRegex().Match(subject ?? "");
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static EmailActionContextDto ToActionContext(BusinessPartner partner, string matchedBy, IReadOnlyList<EmailPartnerSuggestionDto> suggestions) => new(SenderType(partner.Type), matchedBy, new PartnerReference(partner.Id, partner.Name), suggestions);

    private static string SenderType(PartnerType type) => type switch { PartnerType.Carrier => "Carrier", PartnerType.Warehouse => "Warehouse", PartnerType.Collaborator => "Collaborator", PartnerType.Customer => "Customer", _ => "Partner" };

    private static bool IsRelatedOrder(BusinessPartner partner, Guid customerId, Guid? warehouseId, Guid? carrierId) => partner.Type switch { PartnerType.Customer => customerId == partner.Id, PartnerType.Warehouse => warehouseId == partner.Id, PartnerType.Carrier => carrierId == partner.Id, _ => false };

}
