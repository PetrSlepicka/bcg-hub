using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController, Route("api/complaints")]
public sealed class ComplaintsController(IComplaintService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ComplaintListItem>> GetList(CancellationToken cancellationToken) => service.GetListAsync(cancellationToken);
    [HttpGet("{id:guid}")] public async Task<ActionResult<ComplaintDetailDto>> GetDetail(Guid id, CancellationToken cancellationToken) => await service.GetDetailAsync(id, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpPost, ValidateAntiForgeryToken] public async Task<ActionResult<ComplaintDetailDto>> Create(SaveComplaintRequest request, CancellationToken cancellationToken) { var result = await service.CreateAsync(request, cancellationToken); return CreatedAtAction(nameof(GetDetail), new { id = result.Id }, result); }
    [HttpPut("{id:guid}"), ValidateAntiForgeryToken] public async Task<ActionResult<ComplaintDetailDto>> Update(Guid id, SaveComplaintRequest request, CancellationToken cancellationToken) => await service.UpdateAsync(id, request, cancellationToken) is { } result ? Ok(result) : NotFound();
    [HttpDelete("{id:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> Delete(Guid id, uint version, CancellationToken cancellationToken) => await service.DeleteAsync(id, version, cancellationToken) ? NoContent() : NotFound();
}
