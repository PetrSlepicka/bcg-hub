using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class AuthService(BcgHubDbContext db, IPasswordHasher<UserAccount> passwordHasher) : IAuthService
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken);
        if (user is null) return null;
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            user.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return new AuthenticatedUser(user.Id, user.Email, user.FullName);
    }
}
