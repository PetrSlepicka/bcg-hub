using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class UserPasswordGeneratorTests
{
    [Fact]
    public void CreatePassword_ReturnsStrongNonAmbiguousPassword()
    {
        var password = new UserPasswordGenerator().CreatePassword();

        Assert.Equal(18, password.Length);
        Assert.DoesNotContain('0', password);
        Assert.DoesNotContain('O', password);
        Assert.DoesNotContain('I', password);
        Assert.DoesNotContain('l', password);
    }
}
