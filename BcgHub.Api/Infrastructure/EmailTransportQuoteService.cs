using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailTransportQuoteService(BcgHubDbContext db, CurrentUserAccessor currentUser, IOrderCommandService orders, IEmailSenderResolver senderResolver) : IEmailTransportQuoteService
{
    public async Task<EmailTransportQuoteContextDto?> GetContextAsync(Guid emailId, CancellationToken cancellationToken)
    {
        var email = await GetInboundEmailAsync(emailId, cancellationToken);
        if (email is null) return null;
        var sender = await senderResolver.ResolveAsync(email.FromAddress, cancellationToken);
        var linkedPartner = email.BusinessPartnerId.HasValue ? await db.BusinessPartners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == email.BusinessPartnerId, cancellationToken) : null;
        var resolvedPartner = linkedPartner is not null && linkedPartner.Id != sender.Partner?.Id ? linkedPartner : sender.Partner ?? linkedPartner;
        var carrier = resolvedPartner?.Type == PartnerType.Carrier ? resolvedPartner : null;
        if (carrier is null) return null;
        var orderNumber = EmailProcessor.ExtractOrderNumber(email.Subject);
        var availableOrders = await db.Orders.AsNoTracking().OrderByDescending(x => x.OrderedOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new EmailOrderOptionDto(x.Id, x.Number, x.Title, x.Customer.Name)).ToListAsync(cancellationToken);
        var suggestedOrderId = orderNumber is null ? null : availableOrders.FirstOrDefault(x => x.Number == orderNumber)?.Id;
        return new EmailTransportQuoteContextDto(new PartnerReference(carrier.Id, carrier.Name), suggestedOrderId, availableOrders);
    }

    public async Task<TransportQuoteDto?> CreateAsync(Guid emailId, CreateEmailTransportQuoteRequest request, CancellationToken cancellationToken)
    {
        if (request.PickupOn.HasValue && request.DeliveryOn.HasValue && request.DeliveryOn < request.PickupOn) throw new DomainValidationException("Datum doručení nesmí být před datem vyzvednutí.");
        var context = await GetContextAsync(emailId, cancellationToken);
        if (context is null) return null;
        var email = await GetInboundEmailAsync(emailId, cancellationToken);
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? $"Nabídka z e-mailu: {email!.Subject}" : $"{request.Notes.Trim()}\n\nZdroj: e-mail „{email!.Subject}“";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var quote = await orders.AddQuoteAsync(request.OrderId, new SaveTransportQuoteRequest { CarrierId = context.Carrier.Id, Price = request.Price, Currency = request.Currency.Trim().ToUpperInvariant(), PickupOn = request.PickupOn, DeliveryOn = request.DeliveryOn, Notes = notes, IsSelected = false }, cancellationToken);
        if (quote is null) return null;
        await db.EmailMessages.Where(x => x.Id == emailId && x.UserAccountId == currentUser.UserId).ExecuteUpdateAsync(update => update.SetProperty(x => x.BusinessPartnerId, context.Carrier.Id).SetProperty(x => x.OrderId, request.OrderId).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return quote;
    }

    private Task<EmailMessage?> GetInboundEmailAsync(Guid emailId, CancellationToken cancellationToken) => db.EmailMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == emailId && x.UserAccountId == currentUser.UserId && x.Direction == EmailDirection.Inbound, cancellationToken);

}
