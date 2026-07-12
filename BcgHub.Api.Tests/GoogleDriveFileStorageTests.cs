using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class GoogleDriveFileStorageTests
{
    [Fact]
    public void HasOAuthCredentials_RequiresCompleteCredentialSet()
    {
        var options = new GoogleDriveOptions { ClientId = "client", ClientSecret = "secret", RefreshToken = "refresh" };
        Assert.True(GoogleDriveFileStorage.HasOAuthCredentials(options));
        options.RefreshToken = "";
        Assert.False(GoogleDriveFileStorage.HasOAuthCredentials(options));
    }

    [Fact]
    public void HasServiceAccountCredentials_AcceptsJsonOrBase64()
    {
        Assert.True(GoogleDriveFileStorage.HasServiceAccountCredentials(new GoogleDriveOptions { CredentialsJson = "{}" }));
        Assert.True(GoogleDriveFileStorage.HasServiceAccountCredentials(new GoogleDriveOptions { CredentialsBase64 = "e30=" }));
        Assert.False(GoogleDriveFileStorage.HasServiceAccountCredentials(new GoogleDriveOptions()));
    }
}
