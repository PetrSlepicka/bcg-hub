using BcgHub.Api.Application;
using BcgHub.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/emails")]
public sealed class EmailsController(IEmailQueryService queries, IEmailCommandService commands, IEmailSyncService sync, IEmailSender sender, IEmailProcessor processor, IEmailTransportQuoteService transportQuotes, BcgHubDbContext db, CurrentUserAccessor currentUser) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<EmailMessageDto>> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => queries.GetListAsync(q, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmailMessageDto>> Detail(Guid id, CancellationToken cancellationToken)
    {
        await commands.MarkReadAsync(id, cancellationToken);
        var email = await queries.GetDetailAsync(id, cancellationToken);
        if (email is null) return NotFound();
        return Ok(email);
    }

    [HttpPost("sync")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailSyncResultDto>> Sync(CancellationToken cancellationToken) => Ok(new EmailSyncResultDto(await sync.SyncAsync(cancellationToken)));

    [HttpPut("{id:guid}/link")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailMessageDto>> Link(Guid id, LinkEmailRequest request, CancellationToken cancellationToken) => await commands.LinkAsync(id, request, cancellationToken) is { } email ? Ok(email) : NotFound();

    [HttpGet("{id:guid}/order-options")]
    public async Task<ActionResult<EmailOrderOptionsDto>> GetOrderOptions(Guid id, CancellationToken cancellationToken)
    {
        var email = await db.EmailMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        return email is null ? NotFound() : Ok(await processor.GetOrderOptionsAsync(email, cancellationToken));
    }

    [HttpGet("{id:guid}/action-context")]
    public async Task<ActionResult<EmailActionContextDto>> GetActionContext(Guid id, CancellationToken cancellationToken)
    {
        var email = await db.EmailMessages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserAccountId == currentUser.UserId, cancellationToken);
        return email is null ? NotFound() : Ok(await processor.GetActionContextAsync(email, cancellationToken));
    }

    [HttpPost("send")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EmailMessageDto>> Send(SendEmailRequest request, CancellationToken cancellationToken) => Ok(await sender.SendAsync(request, cancellationToken));

    [HttpGet("{id:guid}/transport-quote")]
    public async Task<ActionResult<EmailTransportQuoteContextDto>> GetTransportQuoteContext(Guid id, CancellationToken cancellationToken) => await transportQuotes.GetContextAsync(id, cancellationToken) is { } context ? Ok(context) : NotFound();

    [HttpPost("{id:guid}/transport-quote"), ValidateAntiForgeryToken]
    public async Task<ActionResult<TransportQuoteDto>> CreateTransportQuote(Guid id, CreateEmailTransportQuoteRequest request, CancellationToken cancellationToken) => await transportQuotes.CreateAsync(id, request, cancellationToken) is { } quote ? Ok(quote) : NotFound();
}
