using System.Security.Claims;

namespace BcgHub.Api.Infrastructure;

public sealed class CurrentUserAccessor(IHttpContextAccessor context)
{
    public Guid UserId => Guid.Parse(context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
}
