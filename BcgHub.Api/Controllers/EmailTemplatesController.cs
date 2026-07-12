using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/email-templates")]
public sealed class EmailTemplatesController(IEmailTemplateService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<EmailTemplateDto>> List(CancellationToken cancellationToken) => service.GetAllAsync(cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailTemplateDto>> Create(SaveEmailTemplateRequest request, CancellationToken cancellationToken) => Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailTemplateDto>> Update(Guid id, SaveEmailTemplateRequest request, CancellationToken cancellationToken) => await service.UpdateAsync(id, request, cancellationToken) is { } template ? Ok(template) : NotFound();

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint version, CancellationToken cancellationToken) => await service.DeleteAsync(id, version, cancellationToken) ? NoContent() : NotFound();
}
