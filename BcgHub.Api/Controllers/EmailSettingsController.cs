using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/settings/email")]
public sealed class EmailSettingsController(IEmailSettingsService service, IMicrosoftGraphConnectionService microsoftGraph) : ControllerBase
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

    [HttpPut("provider")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailSettingsDto>> SetProvider(SetEmailProviderRequest request, CancellationToken cancellationToken) => Ok(await service.SetProviderAsync(request.Provider, cancellationToken));

    [HttpGet("microsoft/connect")]
    public IActionResult ConnectMicrosoft([FromQuery] string returnUrl)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/settings/email/microsoft/callback";
        return Redirect(microsoftGraph.CreateAuthorizationUrl(redirectUri, returnUrl));
    }

    [HttpGet("microsoft/callback")]
    public async Task<IActionResult> MicrosoftCallback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/settings/email/microsoft/callback";
        return Redirect(await microsoftGraph.CompleteAuthorizationAsync(code, state, redirectUri, cancellationToken));
    }

    [HttpDelete("microsoft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisconnectMicrosoft(CancellationToken cancellationToken)
    {
        await microsoftGraph.DisconnectAsync(cancellationToken);
        return NoContent();
    }
}
