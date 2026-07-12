using BcgHub.Api.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BcgHub.Api.Controllers;

[ApiController]
[Authorize(Policy = "Superadmin")]
[Route("api/users")]
public sealed class UsersController(IUserManagementService users) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ManagedUserDto>> Get(CancellationToken cancellationToken) => users.GetUsersAsync(cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CreatedManagedUserDto>> Create(CreateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var created = await users.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Update), new { id = created.User.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ManagedUserDto>> Update(Guid id, UpdateManagedUserRequest request, CancellationToken cancellationToken) => await users.UpdateAsync(id, request, cancellationToken) is { } user ? Ok(user) : NotFound();

    [HttpDelete("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) => await users.DeactivateAsync(id, cancellationToken) ? NoContent() : NotFound();
}
