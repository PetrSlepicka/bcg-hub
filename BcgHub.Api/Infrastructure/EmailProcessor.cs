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
        email.BusinessPartnerId = await FindPartnerIdAsync(email.FromAddress, cancellationToken);
        var number = ExtractOrderNumber(email.Subject);
        if (number is not null) email.OrderId = await db.Orders.AsNoTracking().Where(x => x.Number == number).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (email.OrderId.HasValue && !email.BusinessPartnerId.HasValue) email.BusinessPartnerId = await db.Orders.AsNoTracking().Where(x => x.Id == email.OrderId).Select(x => (Guid?)x.CustomerId).SingleAsync(cancellationToken);
    }

    public async Task<EmailOrderOptionsDto> GetOrderOptionsAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        var partnerId = email.BusinessPartnerId ?? await FindPartnerIdAsync(email.FromAddress, cancellationToken);
        var orders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { Option = new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name), x.CustomerId }).ToListAsync(cancellationToken);
        return new EmailOrderOptionsDto(orders.Where(x => partnerId.HasValue && x.CustomerId == partnerId).Select(x => x.Option).ToList(), orders.Where(x => !partnerId.HasValue || x.CustomerId != partnerId).Select(x => x.Option).ToList());
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
}
