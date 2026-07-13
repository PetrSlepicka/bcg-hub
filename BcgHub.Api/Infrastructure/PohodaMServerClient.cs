using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using BcgHub.Api.Application;
using Microsoft.Extensions.Options;

namespace BcgHub.Api.Infrastructure;

public interface IPohodaMServerClient
{
    Task<PohodaMServerResponse> DownloadChangedOrdersAsync(DateTime changedSinceUtc, string runId, CancellationToken cancellationToken);
}

public sealed class PohodaMServerResponse(HttpResponseMessage response, Stream content, long? contentLength) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public long? ContentLength { get; } = contentLength;

    public async ValueTask DisposeAsync() { await Content.DisposeAsync(); response.Dispose(); }
}

public sealed class PohodaMServerClient(IHttpClientFactory httpClientFactory, IOptions<PohodaOptions> options, ILogger<PohodaMServerClient> logger) : IPohodaMServerClient
{
    static PohodaMServerClient() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public async Task<PohodaMServerResponse> DownloadChangedOrdersAsync(DateTime changedSinceUtc, string runId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Validate(settings);
        var endpoint = new Uri(new Uri(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute), "xml");
        var localChangedSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(changedSinceUtc, DateTimeKind.Utc), ResolveTimeZone(settings.TimeZoneId));
        var requestXml = CreateRequest(settings.CompanyNumber, localChangedSince, runId);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("STW-Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"))}");
        request.Headers.TryAddWithoutValidation("STW-Application", "BCG Hub");
        request.Headers.TryAddWithoutValidation("STW-Instance", runId);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        request.Content = new StringContent(requestXml, Encoding.GetEncoding(1250), "text/xml");
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("POHODA mServer request {RunId} started. Endpoint: {Endpoint}, changes since UTC: {ChangedSinceUtc}, changes since POHODA local time: {LocalChangedSince}.", runId, endpoint.GetLeftPart(UriPartial.Authority), changedSinceUtc, localChangedSince);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(Math.Clamp(settings.RequestTimeoutMinutes, 1, 60)));
        var response = await httpClientFactory.CreateClient("PohodaMServer").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(timeout.Token);
            response.Dispose();
            throw new HttpRequestException($"POHODA mServer vrátil HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {Limit(detail, 1000)}");
        }
        var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        logger.LogInformation("POHODA mServer request {RunId} returned HTTP {StatusCode}. Content length: {ContentLength}, headers received in {ElapsedMs} ms.", runId, (int)response.StatusCode, response.Content.Headers.ContentLength, stopwatch.ElapsedMilliseconds);
        return new PohodaMServerResponse(response, stream, response.Content.Headers.ContentLength);
    }

    internal static string CreateRequest(string companyNumber, DateTime localChangedSince, string runId)
    {
        XNamespace dat = "http://www.stormware.cz/schema/version_2/data.xsd";
        XNamespace ftr = "http://www.stormware.cz/schema/version_2/filter.xsd";
        XNamespace lst = "http://www.stormware.cz/schema/version_2/list.xsd";
        var document = new XDocument(new XDeclaration("1.0", "Windows-1250", null), new XElement(dat + "dataPack", new XAttribute("id", runId), new XAttribute("ico", companyNumber), new XAttribute("application", "BCG Hub"), new XAttribute("version", "2.0"), new XAttribute("note", "Automatický export nových nebo změněných přijatých objednávek"), new XAttribute(XNamespace.Xmlns + "dat", dat), new XAttribute(XNamespace.Xmlns + "ftr", ftr), new XAttribute(XNamespace.Xmlns + "lst", lst), new XElement(dat + "dataPackItem", new XAttribute("id", runId), new XAttribute("version", "2.0"), new XElement(lst + "listOrderRequest", new XAttribute("version", "2.0"), new XAttribute("orderType", "receivedOrder"), new XAttribute("orderVersion", "2.0"), new XElement(lst + "requestOrder", new XElement(ftr + "filter", new XElement(ftr + "lastChanges", localChangedSince.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture))))))));
        return document.Declaration + Environment.NewLine + document.Root;
    }

    private static void Validate(PohodaOptions settings)
    {
        if (!settings.Enabled) throw new DomainValidationException("Automatická synchronizace POHODA není povolena.");
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _)) throw new DomainValidationException("Adresa POHODA mServeru není platná.");
        if (string.IsNullOrWhiteSpace(settings.CompanyNumber)) throw new DomainValidationException("Pro POHODA mServer není nastavené IČO účetní jednotky.");
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password)) throw new DomainValidationException("Pro POHODA mServer nejsou nastavené přihlašovací údaje.");
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) when (id == "Europe/Prague") { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
    }

    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
