using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class EmailSenderResolverTests
{
    [Fact]
    public void ResolvePrefersExactAddressOverAnotherPartnerWithSameDomain()
    {
        var exact = Partner(PartnerType.Carrier, "carrier@firma.cz");
        var sameDomain = Partner(PartnerType.Customer, "sales@firma.cz");
        var match = EmailSenderResolver.Resolve([sameDomain, exact], "carrier@firma.cz");
        Assert.Equal(EmailSenderMatchKind.Address, match.Kind);
        Assert.Same(exact, match.Partner);
    }

    [Fact]
    public void ResolveDoesNotMatchPublicMailboxDomain()
    {
        var customer = Partner(PartnerType.Customer, "customer@gmail.com");
        var match = EmailSenderResolver.Resolve([customer], "unknown@gmail.com");
        Assert.Equal(EmailSenderMatchKind.None, match.Kind);
        Assert.Null(match.Partner);
        Assert.Empty(match.Partners);
    }

    [Fact]
    public void ResolveReportsAmbiguousCorporateDomainInsteadOfChoosingArbitrarily()
    {
        var carrier = Partner(PartnerType.Carrier, "transport@firma.cz");
        var warehouse = Partner(PartnerType.Warehouse, "warehouse@firma.cz");
        var match = EmailSenderResolver.Resolve([carrier, warehouse], "new-contact@firma.cz");
        Assert.Equal(EmailSenderMatchKind.Domain, match.Kind);
        Assert.Null(match.Partner);
        Assert.True(match.IsAmbiguous);
        Assert.Equal([carrier, warehouse], match.Partners);
    }

    private static BusinessPartner Partner(PartnerType type, string email) => new() { Id = Guid.NewGuid(), Type = type, Name = email, Email = email };
}
