using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class EmailPartnerSuggestionServiceTests
{
    [Fact]
    public void ExactAddressIsPreferredAndAllExactCandidatesStayVisible()
    {
        var exact = Partner("Přesná shoda");
        var previous = Partner("Předchozí partner");
        var result = EmailPartnerSuggestionService.Create(new EmailSenderMatch([exact], EmailSenderMatchKind.Address), null, previous);
        Assert.Same(exact, result.PreferredPartner);
        Assert.Equal("Address", result.MatchedBy);
        Assert.Equal([exact, previous], result.Candidates);
    }

    [Fact]
    public void AmbiguousMatchesAreSuggestedWithoutPreselection()
    {
        var first = Partner("První");
        var second = Partner("Druhý");
        var result = EmailPartnerSuggestionService.Create(new EmailSenderMatch([first, second], EmailSenderMatchKind.Domain), null, null);
        Assert.Null(result.PreferredPartner);
        Assert.Equal("Ambiguous", result.MatchedBy);
        Assert.Equal([first, second], result.Candidates);
    }

    [Fact]
    public void PreviousAssignmentIsPreferredBeforeSingleDomainMatch()
    {
        var previous = Partner("Předchozí partner");
        var domain = Partner("Shoda domény");
        var result = EmailPartnerSuggestionService.Create(new EmailSenderMatch([domain], EmailSenderMatchKind.Domain), null, previous);
        Assert.Same(previous, result.PreferredPartner);
        Assert.Equal("History", result.MatchedBy);
        Assert.Equal([previous, domain], result.Candidates);
    }

    private static BusinessPartner Partner(string name) => new() { Id = Guid.NewGuid(), Type = PartnerType.Customer, Name = name };
}
