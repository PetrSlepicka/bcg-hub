using System.Globalization;
using System.Xml.Linq;
using BcgHub.Api.Application;

namespace BcgHub.Api.Infrastructure;

public interface IPohodaOrderExportRequestFactory
{
    string Create(string companyNumber, DateTime localChangedSince, string runId);
}

public sealed class PohodaOrderExportRequestFactory : IPohodaOrderExportRequestFactory
{
    public string Create(string companyNumber, DateTime localChangedSince, string runId)
    {
        XNamespace dat = "http://www.stormware.cz/schema/version_2/data.xsd";
        XNamespace ftr = "http://www.stormware.cz/schema/version_2/filter.xsd";
        XNamespace lst = "http://www.stormware.cz/schema/version_2/list.xsd";
        var document = new XDocument(new XDeclaration("1.0", "Windows-1250", null), new XElement(dat + "dataPack", new XAttribute("id", runId), new XAttribute("ico", companyNumber), new XAttribute("application", "BCG Hub"), new XAttribute("version", "2.0"), new XAttribute("note", "Automatický export nových nebo změněných přijatých objednávek"), new XAttribute(XNamespace.Xmlns + "dat", dat), new XAttribute(XNamespace.Xmlns + "ftr", ftr), new XAttribute(XNamespace.Xmlns + "lst", lst), new XElement(dat + "dataPackItem", new XAttribute("id", runId), new XAttribute("version", "2.0"), new XElement(lst + "listOrderRequest", new XAttribute("version", "2.0"), new XAttribute("orderType", "receivedOrder"), new XAttribute("orderVersion", "2.0"), new XElement(lst + "requestOrder", new XElement(ftr + "filter", new XElement(ftr + "lastChanges", localChangedSince.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture))))))));
        PohodaReadOnlyRequestGuard.Validate(document);
        return document.Declaration + Environment.NewLine + document.Root;
    }
}

internal static class PohodaReadOnlyRequestGuard
{
    private static readonly HashSet<string> AllowedElements = ["dataPack", "dataPackItem", "listOrderRequest", "requestOrder", "filter", "lastChanges"];

    public static void Validate(XDocument document)
    {
        var root = document.Root;
        var items = root?.Elements().Where(x => x.Name.LocalName == "dataPackItem").ToList() ?? [];
        var request = items.Count == 1 ? items[0].Elements().SingleOrDefault() : null;
        if (root?.Name.LocalName != "dataPack" || items.Count != 1 || request?.Name.LocalName != "listOrderRequest") throw new DomainValidationException("Bezpečnostní kontrola odmítla POHODA požadavek, protože nejde o jediný exportní požadavek.");
        if (!string.Equals(request.Attribute("orderType")?.Value, "receivedOrder", StringComparison.Ordinal)) throw new DomainValidationException("Bezpečnostní kontrola odmítla POHODA požadavek, protože není omezený na export přijatých objednávek.");
        if (document.Descendants().Any(x => !AllowedElements.Contains(x.Name.LocalName))) throw new DomainValidationException("Bezpečnostní kontrola odmítla POHODA požadavek obsahující nepovolenou operaci.");
        if (document.Descendants().Any(x => x.Attributes().Any(a => a.Name.LocalName is "actionType" or "update" or "delete"))) throw new DomainValidationException("Bezpečnostní kontrola odmítla POHODA požadavek obsahující změnovou operaci.");
    }
}
