using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BcgHub.Api.Application;

public sealed record PohodaCustomerData(string Name, string? CompanyNumber, string? VatNumber, string? Email, string? Phone, string? Street, string? City, string? PostalCode, string? CountryCode);
public sealed record PohodaOrderData(string ExternalId, string? Number, string Title, string OrderType, PohodaCustomerData Customer, DateOnly? OrderedOn, DateOnly? DeliveryOn, decimal ValueCzk);

public interface IPohodaOrderXmlParser { IReadOnlyList<PohodaOrderData> Parse(Stream xml); }

public sealed class PohodaOrderXmlParser : IPohodaOrderXmlParser
{
    static PohodaOrderXmlParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public IReadOnlyList<PohodaOrderData> Parse(Stream xml)
    {
        try
        {
            using var reader = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 250_000_000 });
            var orders = new List<PohodaOrderData>();
            string? accountingUnit = null;
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "responsePack") accountingUnit = reader.GetAttribute("ico");
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "order")
                {
                    var order = (XElement)XNode.ReadFrom(reader);
                    if (Child(order, "orderHeader") is not null) orders.Add(ParseOrder(order, accountingUnit));
                    continue;
                }
                reader.Read();
            }
            if (orders.Count == 0) throw new DomainValidationException("Soubor neobsahuje žádné objednávky ve standardním XML formátu POHODA.");
            return orders;
        }
        catch (DomainValidationException) { throw; }
        catch (XmlException) { throw new DomainValidationException("Soubor není platné XML."); }
        catch (InvalidDataException exception) { throw new DomainValidationException(exception.Message); }
    }

    private static PohodaOrderData ParseOrder(XElement order, string? accountingUnit)
    {
        var header = Child(order, "orderHeader")!;
        var address = Child(Child(header, "partnerIdentity"), "address");
        var requestedNumber = Child(Child(header, "number"), "numberRequested")?.Value;
        var externalId = Child(header, "id")?.Value ?? requestedNumber ?? Child(header, "numberOrder")?.Value;
        if (string.IsNullOrWhiteSpace(externalId)) throw new InvalidDataException("Objednávka v XML nemá interní ID ani číslo dokladu.");
        var number = requestedNumber ?? Child(header, "numberOrder")?.Value;
        var title = Child(header, "text")?.Value ?? number ?? $"Objednávka {externalId}";
        var customerName = Child(address, "company")?.Value ?? Child(address, "name")?.Value ?? "Neznámý zákazník";
        var customer = new PohodaCustomerData(customerName.Trim(), Clean(Child(address, "ico")?.Value), Clean(Child(address, "dic")?.Value), Clean(Child(address, "email")?.Value), Clean(Child(address, "phone")?.Value ?? Child(address, "mobilPhone")?.Value), Clean(Child(address, "street")?.Value), Clean(Child(address, "city")?.Value), Clean(Child(address, "zip")?.Value), Clean(Child(Child(address, "country"), "ids")?.Value));
        var summary = Child(order, "orderSummary");
        var sourceIdentity = string.IsNullOrWhiteSpace(accountingUnit) ? externalId.Trim() : $"{accountingUnit.Trim()}:{externalId.Trim()}";
        return new PohodaOrderData(sourceIdentity, Clean(number), title.Trim(), Child(header, "orderType")?.Value ?? "unknown", customer, Date(Child(header, "date")?.Value), Date(Child(header, "dateTo")?.Value ?? Child(header, "dateDelivery")?.Value), Total(Child(summary, "homeCurrency")));
    }

    private static decimal Total(XElement? homeCurrency)
    {
        if (homeCurrency is null) return 0;
        var total = Decimal(Child(homeCurrency, "priceNone")?.Value) + Decimal(Child(homeCurrency, "priceLowSum")?.Value) + Decimal(Child(homeCurrency, "priceHighSum")?.Value) + Decimal(Child(homeCurrency, "price3Sum")?.Value);
        var round = Child(homeCurrency, "round");
        return total + (Child(round, "priceRound") is { } priceRound ? Decimal(priceRound.Value) : Decimal(Child(round, "priceRoundSum")?.Value) + Decimal(Child(round, "priceRoundSumVAT")?.Value));
    }

    private static XElement? Child(XElement? element, string name) => element?.Elements().FirstOrDefault(x => x.Name.LocalName == name);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateOnly? Date(string? value) => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
    private static decimal Decimal(string? value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
}
