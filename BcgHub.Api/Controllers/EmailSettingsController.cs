using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/settings/email")]
public sealed class EmailSettingsController(IEmailSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmailSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(cancellationToken);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailSettingsDto>> Save(SaveEmailSettingsRequest request, CancellationToken cancellationToken) => Ok(await service.SaveAsync(request, cancellationToken));
}
