using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailCommandService(BcgHubDbContext db, CurrentUserAccessor currentUser) : IEmailCommandService
{
    public async Task<EmailMessageDto?> LinkAsync(Guid id, LinkEmailRequest request, CancellationToken cancellationToken)
    {
        if (request.BusinessPartnerId.HasValue && !await db.BusinessPartners.AnyAsync(x => x.Id == request.BusinessPartnerId, cancellationToken)) throw new DomainValidationException("Vybraný partner neexistuje.");
        if (request.OrderId.HasValue)
        {
            if (!await db.Orders.AnyAsync(x => x.Id == request.OrderId, cancellationToken)) throw new DomainValidationException("Vybraná zakázka neexistuje.");
        }
        var email = await db.EmailMessages.Include(x => x.BusinessPartner).Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        if (email is null) return null;
        db.Entry(email).Property(x => x.Version).OriginalValue = request.Version;
        email.BusinessPartnerId = request.BusinessPartnerId;
        email.OrderId = request.OrderId;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("E-mail mezitím změnil jiný uživatel."); }
        if (email.BusinessPartnerId.HasValue) await db.Entry(email).Reference(x => x.BusinessPartner).LoadAsync(cancellationToken);
        if (email.OrderId.HasValue) await db.Entry(email).Reference(x => x.Order).LoadAsync(cancellationToken);
        return EmailQueryService.Map(email);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken) => await db.EmailMessages.Where(x => x.Id == id && x.UserAccountId == currentUser.UserId && !x.IsRead).ExecuteUpdateAsync(update => update.SetProperty(x => x.IsRead, true).SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
}
