using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class PartnerQueryService(BcgHubDbContext db) : IPartnerQueryService
{
    public async Task<PagedResult<PartnerListItem>> GetListAsync(PartnerType? type, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.BusinessPartners.AsNoTracking();
        if (type is not null) query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search.Trim().Replace("%", "\\%").Replace("_", "\\_")}%", "\\"));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new PartnerListItem(x.Id, x.Type, x.Name, x.City, x.CountryCode, x.Email, x.Phone, x.Contacts.Count)).ToListAsync(cancellationToken);
        return new PagedResult<PartnerListItem>(items, page, pageSize, totalCount);
    }
}
