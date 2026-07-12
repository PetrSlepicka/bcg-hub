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
        var query = db.EmailMessages.AsNoTracking().Where(x => x.UserAccountId == currentUser.UserId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(x => EF.Functions.ILike(x.Subject, pattern, "\\") || EF.Functions.ILike(x.FromAddress, pattern, "\\") || EF.Functions.ILike(x.ToAddress, pattern, "\\") || (x.BodyText != null && EF.Functions.ILike(x.BodyText, pattern, "\\")));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var emails = await query.OrderByDescending(x => x.OccurredAtUtc).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new EmailMessageDto(x.Id, x.Direction.ToString(), x.FromAddress, x.FromName, x.ToAddress, x.Subject, null, null, x.OccurredAtUtc, x.IsRead, x.HasAttachments, x.BusinessPartnerId, x.BusinessPartner != null ? x.BusinessPartner.Name : null, x.OrderId, x.Order != null ? x.Order.Number : null, x.Version)).ToListAsync(cancellationToken);
        return new PagedResult<EmailMessageDto>(emails, page, pageSize, totalCount);
    }

    public async Task<EmailMessageDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.EmailMessages.AsNoTracking().Where(x => x.Id == id && x.UserAccountId == currentUser.UserId).Select(x => new EmailMessageDto(x.Id, x.Direction.ToString(), x.FromAddress, x.FromName, x.ToAddress, x.Subject, x.BodyText, x.BodyHtml, x.OccurredAtUtc, x.IsRead, x.HasAttachments, x.BusinessPartnerId, x.BusinessPartner != null ? x.BusinessPartner.Name : null, x.OrderId, x.Order != null ? x.Order.Number : null, x.Version)).SingleOrDefaultAsync(cancellationToken);
    }

    internal static EmailMessageDto Map(EmailMessage email) => new(email.Id, email.Direction.ToString(), email.FromAddress, email.FromName, email.ToAddress, email.Subject, email.BodyText, email.BodyHtml, email.OccurredAtUtc, email.IsRead, email.HasAttachments, email.BusinessPartnerId, email.BusinessPartner?.Name, email.OrderId, email.Order?.Number, email.Version);
}
