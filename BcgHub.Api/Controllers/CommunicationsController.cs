using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController, Route("api/communications")]
public sealed class CommunicationsController(ICommunicationService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<PagedResult<CommunicationDto>>> GetList(Guid? partnerId, Guid? orderId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) => Ok(await service.GetListAsync(partnerId, orderId, page, pageSize, cancellationToken));
    [HttpPost, ValidateAntiForgeryToken] public async Task<ActionResult<CommunicationDto>> Create(SaveCommunicationRequest request, CancellationToken cancellationToken) { var result = await service.CreateAsync(request, cancellationToken); return Created($"/api/communications/{result.Id}", result); }
    [HttpPut("{id:guid}"), ValidateAntiForgeryToken] public async Task<ActionResult<CommunicationDto>> Update(Guid id, SaveCommunicationRequest request, CancellationToken cancellationToken) { var result = await service.UpdateAsync(id, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
    [HttpDelete("{id:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> Delete(Guid id, uint version, CancellationToken cancellationToken) => await service.DeleteAsync(id, version, cancellationToken) ? NoContent() : NotFound();
}
