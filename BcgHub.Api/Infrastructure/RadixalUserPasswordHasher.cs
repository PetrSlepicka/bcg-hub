using BcgHub.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Radixal.BPC.Security;

namespace BcgHub.Api.Infrastructure;

public sealed class RadixalUserPasswordHasher : IPasswordHasher<UserAccount>
{
    private readonly PasswordHasher<UserAccount> legacyHasher = new();

    public string HashPassword(UserAccount user, string password) => RadixalPasswordHasher.HashPassword(password);

    public PasswordVerificationResult VerifyHashedPassword(UserAccount user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.StartsWith("pbkdf2-sha256:", StringComparison.Ordinal)) return RadixalPasswordHasher.VerifyPassword(providedPassword, hashedPassword) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        return legacyHasher.VerifyHashedPassword(user, hashedPassword, providedPassword) == PasswordVerificationResult.Failed ? PasswordVerificationResult.Failed : PasswordVerificationResult.SuccessRehashNeeded;
    }
}
