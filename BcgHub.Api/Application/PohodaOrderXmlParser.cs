using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BcgHub.Api.Application;

public sealed record PohodaCustomerData(string Name, string? CompanyNumber, string? VatNumber, string? Email, string? Phone, string? Street, string? City, string? PostalCode, string? CountryCode);
public sealed record PohodaOrderData(string ExternalId, string? Number, string Title, string OrderType, PohodaCustomerData Customer, DateOnly? OrderedOn, DateOnly? DeliveryOn, decimal ValueCzk);

public interface IPohodaOrderXmlParser
{
    IReadOnlyList<PohodaOrderData> Parse(Stream xml);
    IReadOnlyList<PohodaOrderData> ParseMServerResponse(Stream xml, string expectedCompanyNumber, string expectedResponseId);
}

public sealed class PohodaOrderXmlParser : IPohodaOrderXmlParser
{
    static PohodaOrderXmlParser() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public IReadOnlyList<PohodaOrderData> Parse(Stream xml) => ParseCore(xml, null, null, false);
    public IReadOnlyList<PohodaOrderData> ParseMServerResponse(Stream xml, string expectedCompanyNumber, string expectedResponseId) => ParseCore(xml, expectedCompanyNumber, expectedResponseId, true);

    private static IReadOnlyList<PohodaOrderData> ParseCore(Stream xml, string? expectedCompanyNumber, string? expectedResponseId, bool strictMServerResponse)
    {
        try
        {
            using var reader = XmlReader.Create(xml, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 250_000_000 });
            var orders = new List<PohodaOrderData>();
            var accountingUnit = expectedCompanyNumber;
            var responsePackSeen = false;
            var listOrderSeen = false;
            var responsePackItemCount = 0;
            int? listOrderDepth = null;
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element && strictMServerResponse && reader.Depth == 0)
                {
                    ValidateResponsePack(reader, expectedCompanyNumber!, expectedResponseId!);
                    responsePackSeen = true;
                    if (!string.IsNullOrWhiteSpace(reader.GetAttribute("ico"))) accountingUnit = reader.GetAttribute("ico");
                }
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "responsePackItem" && strictMServerResponse)
                {
                    responsePackItemCount++;
                    if (reader.NamespaceURI != "http://www.stormware.cz/schema/version_2/response.xsd") throw new InvalidDataException("POHODA mServer vrátil položku odpovědi v neočekávaném XML schématu.");
                    if (responsePackItemCount != 1 || !string.Equals(reader.GetAttribute("id"), expectedResponseId, StringComparison.Ordinal)) throw new InvalidDataException("POHODA mServer vrátil odpověď pro jiný nebo neočekávaný požadavek.");
                }
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "responsePackItem" or "listOrder" && strictMServerResponse && !string.Equals(reader.GetAttribute("state"), "ok", StringComparison.OrdinalIgnoreCase)) throw ResponseError((XElement)XNode.ReadFrom(reader));
                if (reader.NodeType == XmlNodeType.Element && string.Equals(reader.GetAttribute("state"), "error", StringComparison.OrdinalIgnoreCase)) throw ResponseError((XElement)XNode.ReadFrom(reader));
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "responsePack" && !strictMServerResponse && !string.IsNullOrWhiteSpace(reader.GetAttribute("ico"))) accountingUnit = reader.GetAttribute("ico");
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "listOrder" && strictMServerResponse)
                {
                    if (reader.NamespaceURI != "http://www.stormware.cz/schema/version_2/list.xsd") throw new InvalidDataException("Odpověď POHODA mServeru obsahuje seznam objednávek v neočekávaném XML schématu.");
                    if (listOrderSeen) throw new InvalidDataException("POHODA mServer vrátil více seznamů objednávek pro jediný požadavek.");
                    listOrderSeen = true;
                    listOrderDepth = reader.IsEmptyElement ? null : reader.Depth;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "order")
                {
                    if (strictMServerResponse && (!listOrderDepth.HasValue || reader.Depth <= listOrderDepth || reader.NamespaceURI != "http://www.stormware.cz/schema/version_2/order.xsd")) throw new InvalidDataException("Odpověď POHODA mServeru obsahuje objednávku mimo očekávaný exportní seznam.");
                    var order = (XElement)XNode.ReadFrom(reader);
                    if (Child(order, "orderHeader") is not null) orders.Add(ParseOrder(order, accountingUnit, strictMServerResponse));
                    else if (strictMServerResponse) throw new InvalidDataException("Objednávka v odpovědi POHODA mServeru nemá hlavičku.");
                    continue;
                }
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "listOrder" && reader.Depth == listOrderDepth) listOrderDepth = null;
                reader.Read();
            }
            if (strictMServerResponse && (!responsePackSeen || responsePackItemCount != 1 || !listOrderSeen)) throw new InvalidDataException("POHODA mServer nevrátil očekávanou exportní odpověď se seznamem objednávek.");
            if (orders.Count == 0 && !strictMServerResponse) throw new DomainValidationException("Soubor neobsahuje žádné objednávky ve standardním XML formátu POHODA.");
            return orders;
        }
        catch (DomainValidationException) { throw; }
        catch (XmlException) { throw new DomainValidationException("Soubor není platné XML."); }
        catch (InvalidDataException exception) { throw new DomainValidationException(exception.Message); }
    }

    private static void ValidateResponsePack(XmlReader reader, string expectedCompanyNumber, string expectedResponseId)
    {
        if (reader.LocalName != "responsePack" || reader.NamespaceURI != "http://www.stormware.cz/schema/version_2/response.xsd") throw new InvalidDataException("POHODA mServer nevrátil XML responsePack v oficiálním schématu POHODA.");
        if (!string.Equals(reader.GetAttribute("state"), "ok", StringComparison.OrdinalIgnoreCase)) throw ResponseError((XElement)XNode.ReadFrom(reader));
        if (!string.Equals(reader.GetAttribute("id"), expectedResponseId, StringComparison.Ordinal)) throw new InvalidDataException("POHODA mServer vrátil responsePack pro jiný požadavek.");
        var responseCompanyNumber = reader.GetAttribute("ico");
        if (!string.IsNullOrWhiteSpace(responseCompanyNumber) && NormalizeIdentity(responseCompanyNumber) != NormalizeIdentity(expectedCompanyNumber)) throw new InvalidDataException($"POHODA mServer vrátil jinou účetní jednotku (IČO {responseCompanyNumber}) než nakonfigurované IČO {expectedCompanyNumber}.");
    }

    private static DomainValidationException ResponseError(XElement element)
    {
        var detail = element.DescendantsAndSelf().SelectMany(x => new[] { x.Attribute("note")?.Value, x.Name.LocalName is "error" or "message" ? x.Value : null }).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return new DomainValidationException(string.IsNullOrWhiteSpace(detail) ? "POHODA mServer vrátil chybu při exportu objednávek." : $"POHODA mServer vrátil chybu: {detail.Trim()}");
    }

    private static PohodaOrderData ParseOrder(XElement order, string? accountingUnit, bool strict)
    {
        var header = Child(order, "orderHeader")!;
        var address = Child(Child(header, "partnerIdentity"), "address");
        var requestedNumber = Child(Child(header, "number"), "numberRequested")?.Value;
        var internalId = Clean(Child(header, "id")?.Value);
        var externalId = internalId ?? (strict ? null : requestedNumber ?? Child(header, "numberOrder")?.Value);
        if (string.IsNullOrWhiteSpace(externalId)) throw new InvalidDataException("Objednávka v XML nemá interní ID ani číslo dokladu.");
        var number = requestedNumber ?? Child(header, "numberOrder")?.Value;
        var title = Child(header, "text")?.Value ?? number ?? $"Objednávka {externalId}";
        var customerName = Child(address, "company")?.Value ?? Child(address, "name")?.Value ?? "";
        var customer = new PohodaCustomerData(customerName.Trim(), Clean(Child(address, "ico")?.Value), Clean(Child(address, "dic")?.Value), Clean(Child(address, "email")?.Value), Clean(Child(address, "phone")?.Value ?? Child(address, "mobilPhone")?.Value), Clean(Child(address, "street")?.Value), Clean(Child(address, "city")?.Value), Clean(Child(address, "zip")?.Value), Clean(Child(Child(address, "country"), "ids")?.Value));
        var summary = Child(order, "orderSummary");
        var sourceIdentity = string.IsNullOrWhiteSpace(accountingUnit) ? externalId.Trim() : $"{accountingUnit.Trim()}:{externalId.Trim()}";
        return new PohodaOrderData(sourceIdentity, Clean(number), title.Trim(), Child(header, "orderType")?.Value ?? "unknown", customer, Date(Child(header, "date"), strict), Date(Child(header, "dateTo") ?? Child(header, "dateDelivery"), strict), Total(Child(summary, "homeCurrency"), strict));
    }

    private static decimal Total(XElement? homeCurrency, bool strict)
    {
        if (homeCurrency is null)
        {
            if (strict) throw new InvalidDataException("Objednávka v odpovědi POHODA mServeru nemá souhrn v domácí měně.");
            return 0;
        }
        var priceElements = new[] { Child(homeCurrency, "priceNone"), Child(homeCurrency, "priceLowSum"), Child(homeCurrency, "priceHighSum"), Child(homeCurrency, "price3Sum") };
        if (strict && priceElements.All(x => x is null)) throw new InvalidDataException("Objednávka v odpovědi POHODA mServeru nemá žádnou celkovou cenu.");
        var total = priceElements.Sum(x => Decimal(x, strict));
        var round = Child(homeCurrency, "round");
        return total + (Child(round, "priceRound") is { } priceRound ? Decimal(priceRound, strict) : Decimal(Child(round, "priceRoundSum"), strict) + Decimal(Child(round, "priceRoundSumVAT"), strict));
    }

    private static XElement? Child(XElement? element, string name) => element?.Elements().FirstOrDefault(x => x.Name.LocalName == name);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DateOnly? Date(XElement? element, bool strict) { if (element is null || string.IsNullOrWhiteSpace(element.Value)) return null; if (DateOnly.TryParse(element.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed; if (strict) throw new InvalidDataException($"Objednávka v odpovědi POHODA mServeru obsahuje neplatné datum '{element.Value}'."); return null; }
    private static decimal Decimal(XElement? element, bool strict) { if (element is null) return 0; if (decimal.TryParse(element.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) return parsed; if (strict) throw new InvalidDataException($"Objednávka v odpovědi POHODA mServeru obsahuje neplatnou částku '{element.Value}'."); return 0; }
    private static string NormalizeIdentity(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
}
