using System.Text;
using BcgHub.Api.Application;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PohodaOrderXmlParserTests
{
    [Fact]
    public void ParsesStandardPohodaOrderRegardlessOfNamespacePrefix()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <rsp:responsePack ico="12345678" xmlns:rsp="urn:response" xmlns:ord="urn:order" xmlns:typ="urn:type">
              <ord:order>
                <ord:orderHeader>
                  <ord:id>42</ord:id>
                  <ord:orderType>receivedOrder</ord:orderType>
                  <ord:number><typ:numberRequested>OBJ-2026-15</typ:numberRequested></ord:number>
                  <ord:date>2026-07-12</ord:date>
                  <ord:dateTo>2026-07-20</ord:dateTo>
                  <ord:text>Testovací objednávka</ord:text>
                  <ord:partnerIdentity><typ:address><typ:company>Zákazník s.r.o.</typ:company><typ:ico>87654321</typ:ico><typ:dic>CZ87654321</typ:dic><typ:email>nakup@example.cz</typ:email><typ:country><typ:ids>CZ</typ:ids></typ:country></typ:address></ord:partnerIdentity>
                </ord:orderHeader>
                <ord:orderSummary><ord:homeCurrency><typ:priceNone>500</typ:priceNone><typ:priceLowSum>2000.50</typ:priceLowSum><typ:priceHighSum>10000</typ:priceHighSum><typ:round><typ:priceRound>0</typ:priceRound></typ:round></ord:homeCurrency></ord:orderSummary>
              </ord:order>
            </rsp:responsePack>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var order = Assert.Single(new PohodaOrderXmlParser().Parse(stream));

        Assert.Equal("12345678:42", order.ExternalId);
        Assert.Equal("OBJ-2026-15", order.Number);
        Assert.Equal("receivedOrder", order.OrderType);
        Assert.Equal("Zákazník s.r.o.", order.Customer.Name);
        Assert.Equal("87654321", order.Customer.CompanyNumber);
        Assert.Equal("CZ87654321", order.Customer.VatNumber);
        Assert.Equal("CZ", order.Customer.CountryCode);
        Assert.Equal(new DateOnly(2026, 7, 12), order.OrderedOn);
        Assert.Equal(new DateOnly(2026, 7, 20), order.DeliveryOn);
        Assert.Equal(12500.50m, order.ValueCzk);
    }

    [Fact]
    public void RejectsXmlWithoutOrders()
    {
        using var stream = new MemoryStream("<root />"u8.ToArray());
        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().Parse(stream));
        Assert.Equal("Soubor neobsahuje žádné objednávky ve standardním XML formátu POHODA.", exception.Message);
    }

    [Fact]
    public void ReadsWindows1250EncodingUsedByOfficialExport()
    {
        var parser = new PohodaOrderXmlParser();
        const string xml = "<?xml version=\"1.0\" encoding=\"Windows-1250\"?><responsePack ico=\"12345678\"><order><orderHeader><id>7</id><orderType>receivedOrder</orderType><number><numberRequested>OBJ-7</numberRequested></number><text>Česká objednávka</text><partnerIdentity><address><company>Žluťoučký zákazník</company><ico>87654321</ico></address></partnerIdentity></orderHeader></order></responsePack>";
        using var stream = new MemoryStream(Encoding.GetEncoding(1250).GetBytes(xml));

        var order = Assert.Single(parser.Parse(stream));

        Assert.Equal("Česká objednávka", order.Title);
        Assert.Equal("Žluťoučký zákazník", order.Customer.Name);
    }

    [Fact]
    public void UsesConfiguredCompanyNumberWhenMServerResponseDoesNotContainIco()
    {
        const string xml = "<rsp:responsePack id=\"run1\" state=\"ok\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\" xmlns:lst=\"http://www.stormware.cz/schema/version_2/list.xsd\" xmlns:ord=\"http://www.stormware.cz/schema/version_2/order.xsd\"><rsp:responsePackItem id=\"run1\" state=\"ok\"><lst:listOrder state=\"ok\"><ord:order><ord:orderHeader><ord:id>42</ord:id><ord:orderType>receivedOrder</ord:orderType><ord:partnerIdentity><ord:address><ord:company>Zákazník</ord:company></ord:address></ord:partnerIdentity></ord:orderHeader><ord:orderSummary><ord:homeCurrency><ord:priceNone>0</ord:priceNone></ord:homeCurrency></ord:orderSummary></ord:order></lst:listOrder></rsp:responsePackItem></rsp:responsePack>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var order = Assert.Single(new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Equal("71726462:42", order.ExternalId);
    }

    [Fact]
    public void AllowsSuccessfulMServerResponseWithoutChangedOrders()
    {
        using var stream = new MemoryStream("<rsp:responsePack id=\"run1\" state=\"ok\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\" xmlns:lst=\"http://www.stormware.cz/schema/version_2/list.xsd\"><rsp:responsePackItem id=\"run1\" state=\"ok\"><lst:listOrder state=\"ok\" /></rsp:responsePackItem></rsp:responsePack>"u8.ToArray());

        var orders = new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1");

        Assert.Empty(orders);
    }

    [Fact]
    public void RejectsMServerErrorResponseEvenWhenEmptyResponsesAreAllowed()
    {
        using var stream = new MemoryStream("<rsp:responsePack id=\"run1\" state=\"ok\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\"><rsp:responsePackItem id=\"run1\" state=\"error\" note=\"Neplatné přihlášení\" /></rsp:responsePack>"u8.ToArray());

        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Contains("Neplatné přihlášení", exception.Message);
    }

    [Fact]
    public void RejectsSuccessfulHttpBodyThatIsNotOfficialMServerResponse()
    {
        using var stream = new MemoryStream("<html><body>Proxy login</body></html>"u8.ToArray());

        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Contains("responsePack", exception.Message);
    }

    [Fact]
    public void RejectsResponseForDifferentAccountingUnit()
    {
        using var stream = new MemoryStream("<rsp:responsePack id=\"run1\" state=\"ok\" ico=\"99999999\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\" xmlns:lst=\"http://www.stormware.cz/schema/version_2/list.xsd\"><rsp:responsePackItem id=\"run1\" state=\"ok\"><lst:listOrder state=\"ok\" /></rsp:responsePackItem></rsp:responsePack>"u8.ToArray());

        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Contains("jinou účetní jednotku", exception.Message);
    }

    [Fact]
    public void RejectsResponseBelongingToDifferentRequest()
    {
        using var stream = new MemoryStream("<rsp:responsePack id=\"old-run\" state=\"ok\" ico=\"71726462\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\" xmlns:lst=\"http://www.stormware.cz/schema/version_2/list.xsd\"><rsp:responsePackItem id=\"old-run\" state=\"ok\"><lst:listOrder state=\"ok\" /></rsp:responsePackItem></rsp:responsePack>"u8.ToArray());

        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Contains("jiný požadavek", exception.Message);
    }

    [Fact]
    public void RejectsInvalidAmountInsteadOfReplacingItWithZero()
    {
        const string xml = "<rsp:responsePack id=\"run1\" state=\"ok\" ico=\"71726462\" xmlns:rsp=\"http://www.stormware.cz/schema/version_2/response.xsd\" xmlns:lst=\"http://www.stormware.cz/schema/version_2/list.xsd\" xmlns:ord=\"http://www.stormware.cz/schema/version_2/order.xsd\"><rsp:responsePackItem id=\"run1\" state=\"ok\"><lst:listOrder state=\"ok\"><ord:order><ord:orderHeader><ord:id>42</ord:id><ord:orderType>receivedOrder</ord:orderType><ord:partnerIdentity><ord:address><ord:company>Zákazník</ord:company></ord:address></ord:partnerIdentity></ord:orderHeader><ord:orderSummary><ord:homeCurrency><ord:priceNone>neplatná částka</ord:priceNone></ord:homeCurrency></ord:orderSummary></ord:order></lst:listOrder></rsp:responsePackItem></rsp:responsePack>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var exception = Assert.Throws<DomainValidationException>(() => new PohodaOrderXmlParser().ParseMServerResponse(stream, "71726462", "run1"));

        Assert.Contains("neplatnou částku", exception.Message);
    }
}
