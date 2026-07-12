using BcgHub.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController, Route("api/resources")]
public sealed class ResourcesController(IEntityResourceService service) : ControllerBase
{
    [HttpGet("{ownerType}/{ownerId:guid}/comments")] public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken) => Ok(await service.GetCommentsAsync(ownerType, ownerId, cancellationToken));
    [HttpPost("{ownerType}/{ownerId:guid}/comments"), ValidateAntiForgeryToken] public async Task<ActionResult<CommentDto>> AddComment(ResourceOwnerType ownerType, Guid ownerId, SaveCommentRequest request, CancellationToken cancellationToken) { var result = await service.AddCommentAsync(ownerType, ownerId, request, cancellationToken); return CreatedAtAction(nameof(GetComments), new { ownerType, ownerId }, result); }
    [HttpPut("comments/{id:guid}"), ValidateAntiForgeryToken] public async Task<ActionResult<CommentDto>> UpdateComment(Guid id, SaveCommentRequest request, CancellationToken cancellationToken) { var result = await service.UpdateCommentAsync(id, request, cancellationToken); return result is null ? NotFound() : Ok(result); }
    [HttpDelete("comments/{id:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> DeleteComment(Guid id, uint version, CancellationToken cancellationToken) => await service.DeleteCommentAsync(id, version, cancellationToken) ? NoContent() : NotFound();
    [HttpGet("{ownerType}/{ownerId:guid}/attachments")] public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetAttachments(ResourceOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken) => Ok(await service.GetAttachmentsAsync(ownerType, ownerId, cancellationToken));
    [HttpPost("{ownerType}/{ownerId:guid}/attachments"), ValidateAntiForgeryToken, RequestSizeLimit(2 * 1024 * 1024)] public async Task<ActionResult<AttachmentDto>> AddAttachment(ResourceOwnerType ownerType, Guid ownerId, IFormFile file, CancellationToken cancellationToken) { if (file.Length == 0) return BadRequest(); await using var stream = file.OpenReadStream(); var result = await service.AddAttachmentAsync(ownerType, ownerId, file.FileName, file.ContentType, file.Length, stream, cancellationToken); return CreatedAtAction(nameof(DownloadAttachment), new { id = result.Id }, result); }
    [HttpGet("attachments/{id:guid}/content")] public async Task<IActionResult> DownloadAttachment(Guid id, CancellationToken cancellationToken) { var result = await service.OpenAttachmentAsync(id, cancellationToken); return result is null ? NotFound() : File(result.Stream, result.ContentType, result.FileName); }
    [HttpDelete("attachments/{id:guid}"), ValidateAntiForgeryToken] public async Task<IActionResult> DeleteAttachment(Guid id, uint version, CancellationToken cancellationToken) => await service.DeleteAttachmentAsync(id, version, cancellationToken) ? NoContent() : NotFound();
}
