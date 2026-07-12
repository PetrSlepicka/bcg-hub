using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class RadixalUserPasswordHasherTests
{
    private readonly UserAccount user = new();
    private readonly RadixalUserPasswordHasher hasher = new();

    [Fact]
    public void HashPassword_CreatesVerifiableRadixalHash()
    {
        var hash = hasher.HashPassword(user, "Strong password 123!");

        Assert.StartsWith("pbkdf2-sha256:", hash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, hash, "Strong password 123!"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, hash, "wrong password"));
    }

    [Fact]
    public void VerifyHashedPassword_AcceptsLegacyIdentityHashForRehash()
    {
        var legacyHash = new PasswordHasher<UserAccount>().HashPassword(user, "Strong password 123!");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, hasher.VerifyHashedPassword(user, legacyHash, "Strong password 123!"));
    }
}
