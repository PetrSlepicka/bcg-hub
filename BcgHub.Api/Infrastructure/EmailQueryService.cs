using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class EmailQueryService(BcgHubDbContext db, CurrentUserAccessor currentUser) : IEmailQueryService
{
    public async Task<PagedResult<EmailMessageDto>> GetListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.EmailMessages.AsNoTracking().Include(x => x.BusinessPartner).Include(x => x.Order).Where(x => x.UserAccountId == currentUser.UserId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x => EF.Functions.ILike(x.Subject, pattern, "\\") || EF.Functions.ILike(x.FromAddress, pattern, "\\") || EF.Functions.ILike(x.ToAddress, pattern, "\\") || (x.BodyText != null && EF.Functions.ILike(x.BodyText, pattern, "\\")));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var emails = await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<EmailMessageDto>(emails.Select(Map).ToList(), page, pageSize, totalCount);
    }

    public async Task<EmailMessageDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var email = await db.EmailMessages.AsNoTracking().Include(x => x.BusinessPartner).Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        return email is null ? null : Map(email);
    }

    internal static EmailMessageDto Map(EmailMessage email) => new(email.Id, email.Direction.ToString(), email.FromAddress, email.FromName, email.ToAddress, email.Subject, email.BodyText, email.BodyHtml, email.OccurredAtUtc, email.IsRead, email.HasAttachments, email.BusinessPartnerId, email.BusinessPartner?.Name, email.OrderId, email.Order?.Number, email.Version);
}
