using BcgHub.Api.Infrastructure;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class TransportInquiryServiceTests
{
    [Fact]
    public void EnsureOrderNumberAddsRecognizableOrderNumberWhenTemplateOmitsIt()
    {
        var subject = TransportInquiryService.EnsureOrderNumber("Poptávka pozemní dopravy", "BCG_20260042");
        Assert.Equal("[BCG_20260042] Poptávka pozemní dopravy", subject);
    }

    [Fact]
    public void EnsureOrderNumberDoesNotDuplicateExistingOrderNumber()
    {
        var subject = TransportInquiryService.EnsureOrderNumber("Poptávka BCG_20260042", "BCG_20260042");
        Assert.Equal("Poptávka BCG_20260042", subject);
    }
}
