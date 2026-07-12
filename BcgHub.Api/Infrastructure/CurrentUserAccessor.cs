using System.Security.Claims;
using Radixal.BPC.Logging;

namespace BcgHub.Api.Infrastructure;

public sealed class CurrentUserAccessor(IHttpContextAccessor context) : ICurrentOperationUserAccessor
{
    public Guid UserId => Guid.Parse(context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
    public string FullName => context.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? throw new UnauthorizedAccessException();

    public Task<OperationUser?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var principal = context.HttpContext?.User;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<OperationUser?>(null);
        return Task.FromResult<OperationUser?>(new OperationUser(id, principal?.FindFirstValue(ClaimTypes.Email), principal?.FindFirstValue(ClaimTypes.Name)));
    }
}
