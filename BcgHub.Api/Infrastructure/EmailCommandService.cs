using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailCommandService(BcgHubDbContext db, CurrentUserAccessor currentUser) : IEmailCommandService
{
    public async Task<EmailMessageDto?> LinkAsync(Guid id, LinkEmailRequest request, CancellationToken cancellationToken)
    {
        if (!request.OrderId.HasValue && request.BusinessPartnerId.HasValue && !await db.BusinessPartners.AnyAsync(x => x.Id == request.BusinessPartnerId, cancellationToken)) throw new DomainValidationException("Vybraný partner neexistuje.");
        Guid? orderCustomerId = null;
        if (request.OrderId.HasValue)
        {
            orderCustomerId = await db.Orders.Where(x => x.Id == request.OrderId).Select(x => (Guid?)x.CustomerId).SingleOrDefaultAsync(cancellationToken);
            if (!orderCustomerId.HasValue) throw new DomainValidationException("Vybraná zakázka neexistuje.");
        }
        var email = await db.EmailMessages.Include(x => x.BusinessPartner).Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        if (email is null) return null;
        db.Entry(email).Property(x => x.Version).OriginalValue = request.Version;
        email.BusinessPartnerId = orderCustomerId ?? request.BusinessPartnerId;
        email.OrderId = request.OrderId;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("E-mail mezitím změnil jiný uživatel."); }
        await LinkHistoricalEmailsAsync(email, cancellationToken);
        if (email.BusinessPartnerId.HasValue) await db.Entry(email).Reference(x => x.BusinessPartner).LoadAsync(cancellationToken);
        if (email.OrderId.HasValue) await db.Entry(email).Reference(x => x.Order).LoadAsync(cancellationToken);
        return EmailQueryService.Map(email);
    }

    private async Task LinkHistoricalEmailsAsync(EmailMessage source, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(source.FromAddress) || (!source.BusinessPartnerId.HasValue && !source.OrderId.HasValue)) return;
        var address = source.FromAddress.Trim().ToLower();
        var candidates = db.EmailMessages.Where(x => x.Id != source.Id && x.UserAccountId == source.UserAccountId && x.FromAddress.ToLower() == address);
        if (source.OrderId.HasValue)
        {
            candidates = candidates.Where(x => (!x.BusinessPartnerId.HasValue || x.BusinessPartnerId == source.BusinessPartnerId) && (!x.OrderId.HasValue || x.OrderId == source.OrderId));
            await candidates.ExecuteUpdateAsync(update => update.SetProperty(x => x.BusinessPartnerId, x => x.BusinessPartnerId ?? source.BusinessPartnerId).SetProperty(x => x.OrderId, x => x.OrderId ?? source.OrderId).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
            return;
        }
        await candidates.Where(x => !x.BusinessPartnerId.HasValue && !x.OrderId.HasValue).ExecuteUpdateAsync(update => update.SetProperty(x => x.BusinessPartnerId, source.BusinessPartnerId).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken) => await db.EmailMessages.Where(x => x.Id == id && x.UserAccountId == currentUser.UserId && !x.IsRead).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsRead, true).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
}
