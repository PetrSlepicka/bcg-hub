using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Route("api/partners")]
public sealed class PartnersController(IPartnerService partners) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<PartnerListItem>> GetList([FromQuery] PartnerType? type = null, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default) => partners.GetListAsync(type, search, page, pageSize, cancellationToken);
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PartnerDetailDto>> GetDetail(Guid id, CancellationToken cancellationToken) => await partners.GetDetailAsync(id, cancellationToken) is { } partner ? Ok(partner) : NotFound();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult<PartnerDetailDto>> Create(SavePartnerRequest request, CancellationToken cancellationToken) { var partner = await partners.CreateAsync(request, cancellationToken); return CreatedAtAction(nameof(GetDetail), new { id = partner.Id }, partner); }
    [HttpPut("{id:guid}"), ValidateAntiForgeryToken]
    public async Task<ActionResult<PartnerDetailDto>> Update(Guid id, SavePartnerRequest request, CancellationToken cancellationToken) => await partners.UpdateAsync(id, request, cancellationToken) is { } partner ? Ok(partner) : NotFound();
    [HttpDelete("{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint version, CancellationToken cancellationToken) => await partners.DeleteAsync(id, version, cancellationToken) ? NoContent() : NotFound();
    [HttpPost("{partnerId:guid}/contacts"), ValidateAntiForgeryToken]
    public async Task<ActionResult<ContactPersonDto>> AddContact(Guid partnerId, SaveContactPersonRequest request, CancellationToken cancellationToken) => await partners.AddContactAsync(partnerId, request, cancellationToken) is { } contact ? Ok(contact) : NotFound();
    [HttpPut("{partnerId:guid}/contacts/{contactId:guid}"), ValidateAntiForgeryToken]
    public async Task<ActionResult<ContactPersonDto>> UpdateContact(Guid partnerId, Guid contactId, SaveContactPersonRequest request, CancellationToken cancellationToken) => await partners.UpdateContactAsync(partnerId, contactId, request, cancellationToken) is { } contact ? Ok(contact) : NotFound();
    [HttpDelete("{partnerId:guid}/contacts/{contactId:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContact(Guid partnerId, Guid contactId, [FromQuery] uint version, CancellationToken cancellationToken) => await partners.DeleteContactAsync(partnerId, contactId, version, cancellationToken) ? NoContent() : NotFound();
}
