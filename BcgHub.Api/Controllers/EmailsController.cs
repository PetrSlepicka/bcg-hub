using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/emails")]
public sealed class EmailsController(IEmailQueryService queries, IEmailCommandService commands, IEmailSyncService sync) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<EmailMessageDto>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => queries.GetListAsync(q, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmailMessageDto>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var email = await queries.GetDetailAsync(id, cancellationToken);
        if (email is null) return NotFound();
        await commands.MarkReadAsync(id, cancellationToken);
        return Ok(email with { IsRead = true });
    }

    [HttpPost("sync")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailSyncResultDto>> Sync(CancellationToken cancellationToken) => Ok(new EmailSyncResultDto(await sync.SyncAsync(cancellationToken)));

    [HttpPut("{id:guid}/link")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailMessageDto>> Link(Guid id, LinkEmailRequest request, CancellationToken cancellationToken) => await commands.LinkAsync(id, request, cancellationToken) is { } email ? Ok(email) : NotFound();
}
