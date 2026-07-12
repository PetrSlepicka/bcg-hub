using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PartnerPrimaryContactSynchronizerTests
{
    [Fact]
    public void CreatesPrimaryContactFromPartnerContactDetails()
    {
        var partner = new BusinessPartner { Name = "Acme", Email = "info@acme.cz", Phone = "+420 123 456 789" };

        PartnerPrimaryContactSynchronizer.Synchronize(partner);

        var contact = Assert.Single(partner.Contacts);
        Assert.Equal("Acme", contact.FullName);
        Assert.Equal(partner.Email, contact.Email);
        Assert.Equal(partner.Phone, contact.Phone);
        Assert.True(contact.IsPrimary);
    }

    [Fact]
    public void UpdatesExistingPrimaryContactAndPreservesItsIdentity()
    {
        var contact = new ContactPerson { FullName = "Jan Novák", Position = "Nákup", Email = "old@acme.cz", IsPrimary = true };
        var partner = new BusinessPartner { Name = "Acme", Email = "new@acme.cz", Phone = "+420 987 654 321", Contacts = [contact] };

        PartnerPrimaryContactSynchronizer.Synchronize(partner);

        Assert.Same(contact, Assert.Single(partner.Contacts));
        Assert.Equal("Jan Novák", contact.FullName);
        Assert.Equal("Nákup", contact.Position);
        Assert.Equal(partner.Email, contact.Email);
        Assert.Equal(partner.Phone, contact.Phone);
    }

    [Fact]
    public void DoesNotCreateContactWithoutEmailOrPhone()
    {
        var partner = new BusinessPartner { Name = "Acme" };

        PartnerPrimaryContactSynchronizer.Synchronize(partner);

        Assert.Empty(partner.Contacts);
    }
}
