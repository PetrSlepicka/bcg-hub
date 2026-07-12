using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class UserManagementService(BcgHubDbContext db, CurrentUserAccessor currentUser, IPasswordHasher<UserAccount> passwordHasher, IUserPasswordGenerator passwordGenerator) : IUserManagementService
{
    public async Task<IReadOnlyList<ManagedUserDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        return await db.Users.AsNoTracking().OrderByDescending(x => x.IsActive).ThenBy(x => x.FullName).Select(x => new ManagedUserDto(x.Id, x.FullName, x.Email, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc, x.Id == currentUserId)).ToListAsync(cancellationToken);
    }

    public async Task<CreatedManagedUserDto> CreateAsync(CreateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw new DomainValidationException("Uživatel s tímto e-mailem už existuje.");
        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password) ? passwordGenerator.CreatePassword() : request.Password!;
        var user = new UserAccount { Email = email, FullName = request.FullName.Trim() };
        user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return new CreatedManagedUserDto(ToDto(user, currentUser.UserId), temporaryPassword);
    }

    public async Task<ManagedUserDto?> UpdateAsync(Guid id, UpdateManagedUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return null;
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email == email, cancellationToken)) throw new DomainValidationException("Uživatel s tímto e-mailem už existuje.");
        if (id == currentUser.UserId && !request.IsActive) throw new DomainValidationException("Aktuálně přihlášený účet nelze deaktivovat.");
        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password)) user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(user, currentUser.UserId);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == currentUser.UserId) throw new DomainValidationException("Aktuálně přihlášený účet nelze deaktivovat.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return false;
        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ManagedUserDto ToDto(UserAccount user, Guid currentUserId) => new(user.Id, user.FullName, user.Email, user.IsActive, user.CreatedAtUtc, user.UpdatedAtUtc, user.Id == currentUserId);
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
