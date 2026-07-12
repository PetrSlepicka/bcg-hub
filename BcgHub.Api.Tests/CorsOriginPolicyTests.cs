using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class CorsOriginPolicyTests
{
    [Fact]
    public void AllowsConfiguredLoopbackOrigin() => Assert.True(CorsOriginPolicy.IsAllowed("http://localhost:5173", ["http://localhost:5173"]));

    [Theory]
    [InlineData("https://localhost:4173")]
    [InlineData("http://127.0.0.1:3000")]
    [InlineData("http://[::1]:5173")]
    public void RejectsUnconfiguredLoopbackOrigins(string origin) => Assert.False(CorsOriginPolicy.IsAllowed(origin, ["http://localhost:5173"]));

    [Fact]
    public void AllowsExplicitProductionOrigin() => Assert.True(CorsOriginPolicy.IsAllowed("https://dev.radixal.net", ["https://dev.radixal.net"]));

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("file:///tmp/index.html")]
    [InlineData("not-an-origin")]
    public void RejectsUntrustedOrigins(string origin) => Assert.False(CorsOriginPolicy.IsAllowed(origin, ["https://dev.radixal.net"]));
}
