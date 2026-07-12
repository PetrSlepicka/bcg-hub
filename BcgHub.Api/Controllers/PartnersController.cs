using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/partners")]
public sealed class PartnersController(IPartnerQueryService partners) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PartnerListItem>> GetList([FromQuery] PartnerType? type = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => partners.GetListAsync(type, search, page, pageSize, cancellationToken);
}
