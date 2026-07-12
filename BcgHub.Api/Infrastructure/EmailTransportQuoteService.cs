using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailTransportQuoteService(BcgHubDbContext db, CurrentUserAccessor currentUser, IOrderCommandService orders) : IEmailTransportQuoteService
{
    public async Task<EmailTransportQuoteContextDto?> GetContextAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var email = await GetInboundEmailAsync(emailId, cancellationToken);
        if (email is null) return null;
        var carrier = await FindCarrierByDomainAsync(email.FromAddress, cancellationToken);
        if (carrier is null) return null;
        var orderNumber = EmailProcessor.ExtractOrderNumber(email.Subject);
        var availableOrders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name)).ToListAsync(cancellationToken);
        var suggestedOrderId = orderNumber is null ? null : availableOrders.FirstOrDefault(x => x.Number == orderNumber)?.Id;
        return new EmailTransportQuoteContextDto(new PartnerReference(carrier.Id, carrier.Name), suggestedOrderId, availableOrders);
    }

    public async Task<TransportQuoteDto?> CreateAsync(Guid emailId, CreateEmailTransportQuoteRequest request, CancellationToken cancellationToken)
    {
        var context = await GetContextAsync(emailId, cancellationToken);
        if (context is null) return null;
        var email = await GetInboundEmailAsync(emailId, cancellationToken);
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? $"Nabídka z e-mailu: {email!.Subject}" : $"{request.Notes.Trim()}\n\nZdroj: e-mail „{email!.Subject}“";
        return await orders.AddQuoteAsync(request.OrderId, new SaveTransportQuoteRequest { CarrierId = context.Carrier.Id, Price = request.Price, Currency = request.Currency.Trim().ToUpperInvariant(), PickupOn = request.PickupOn, DeliveryOn = request.DeliveryOn, Notes = notes, IsSelected = false }, cancellationToken);
    }

    private Task<EmailMessage?> GetInboundEmailAsync(Guid emailId, CancellationToken cancellationToken) => db.EmailMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == emailId && x.UserAccountId == currentUser.UserId && x.Direction == EmailDirection.Inbound, cancellationToken);

    private async Task<BusinessPartner?> FindCarrierByDomainAsync(string address, CancellationToken cancellationToken)
    {
        var domain = GetDomain(address);
        if (domain is null) return null;
        var carriers = await db.BusinessPartners.AsNoTracking().Include(x => x.Contacts).Where(x => x.Type == PartnerType.Carrier).ToListAsync(cancellationToken);
        return carriers.FirstOrDefault(carrier => GetDomain(carrier.Email) == domain || carrier.Contacts.Any(contact => GetDomain(contact.Email) == domain));
    }

    internal static string? GetDomain(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var separator = address.LastIndexOf('@');
        return separator > 0 && separator < address.Length - 1 ? address[(separator + 1)..].Trim().ToLowerInvariant() : null;
    }
}
