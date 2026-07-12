using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BcgHub.Api.Infrastructure;

public sealed class UserAccountSeeder(BcgHubDbContext db, IPasswordHasher<UserAccount> passwordHasher, BootstrapAdminOptions bootstrap, ILogger<UserAccountSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bootstrap.Email) || string.IsNullOrWhiteSpace(bootstrap.Password)) return;
        if (bootstrap.Password.Length < 12) throw new InvalidOperationException("BootstrapAdmin password must contain at least 12 characters.");
        var email = bootstrap.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) return;
        var user = new UserAccount { Email = email, FullName = string.IsNullOrWhiteSpace(bootstrap.FullName) ? email : bootstrap.FullName.Trim() };
        user.PasswordHash = passwordHasher.HashPassword(user, bootstrap.Password);
        db.Users.Add(user);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (!await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) throw;
        }
        logger.LogInformation("Bootstrap administrator {Email} is available.", email);
    }
}
