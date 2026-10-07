using System.Diagnostics;
using System.Text;
using BcgHub.Api.Application;
using Microsoft.Extensions.Options;

namespace BcgHub.Api.Infrastructure;

public interface IPohodaMServerClient
{
    Task<PohodaMServerResponse> DownloadChangedOrdersAsync(DateTime changedSinceUtc, string runId, CancellationToken cancellationToken);
}

public sealed class PohodaMServerResponse(Stream content, long contentLength) : IAsyncDisposable
{
    public Stream Content { get; } = content;
    public long ContentLength { get; } = contentLength;

    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public sealed class PohodaMServerClient(IHttpClientFactory httpClientFactory, IPohodaOrderExportRequestFactory requestFactory, IOptions<PohodaOptions> options, ILogger<PohodaMServerClient> logger) : IPohodaMServerClient
{
    static PohodaMServerClient() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public async Task<PohodaMServerResponse> DownloadChangedOrdersAsync(DateTime changedSinceUtc, string runId, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        Validate(settings);
        var endpoint = new Uri(new Uri(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute), "xml");
        var localChangedSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(changedSinceUtc, DateTimeKind.Utc), ResolveTimeZone(settings.TimeZoneId));
        var requestXml = requestFactory.Create(settings.CompanyNumber, localChangedSince, runId);
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
        try
        {
            using var response = await httpClientFactory.CreateClient("PohodaMServer").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(timeout.Token);
                throw new HttpRequestException($"POHODA mServer vrátil HTTP {(int)response.StatusCode} {response.ReasonPhrase}. {Limit(detail, 1000)}");
            }
            if (!string.Equals(response.Content.Headers.ContentType?.MediaType, "text/xml", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"POHODA mServer vrátil neočekávaný Content-Type '{response.Content.Headers.ContentType?.MediaType ?? "neuveden"}'.");
            var maximumBytes = Math.Clamp(settings.MaxResponseBytes, 1_000_000, 250_000_000);
            if (response.Content.Headers.ContentLength > maximumBytes) throw new InvalidDataException($"POHODA mServer vrátil odpověď větší než povolených {maximumBytes} bajtů.");
            await using var source = await response.Content.ReadAsStreamAsync(timeout.Token);
            var buffered = CreateTemporaryStream();
            try
            {
                var downloadedBytes = await CopyWithLimitAsync(source, buffered, maximumBytes, timeout.Token);
                buffered.Position = 0;
                logger.LogInformation("POHODA mServer request {RunId} returned HTTP {StatusCode}. Downloaded {DownloadedBytes} bytes in {ElapsedMs} ms.", runId, (int)response.StatusCode, downloadedBytes, stopwatch.ElapsedMilliseconds);
                return new PohodaMServerResponse(buffered, downloadedBytes);
            }
            catch
            {
                await buffered.DisposeAsync();
                throw;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new TimeoutException($"POHODA mServer nedokončil odpověď do {Math.Clamp(settings.RequestTimeoutMinutes, 1, 60)} minut."); }
    }

    private static void Validate(PohodaOptions settings)
    {
        if (!settings.Enabled) throw new DomainValidationException("Automatická synchronizace POHODA není povolena.");
        if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo)) throw new DomainValidationException("Adresa POHODA mServeru není platná HTTP adresa bez vložených přihlašovacích údajů.");
        if (string.IsNullOrWhiteSpace(settings.CompanyNumber)) throw new DomainValidationException("Pro POHODA mServer není nastavené IČO účetní jednotky.");
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password)) throw new DomainValidationException("Pro POHODA mServer nejsou nastavené přihlašovací údaje.");
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) when (id == "Europe/Prague") { return TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); }
    }

    private static FileStream CreateTemporaryStream() => new(Path.Combine(Path.GetTempPath(), $"bcghub-pohoda-{Guid.NewGuid():N}.xml"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81_920, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

    private static async Task<long> CopyWithLimitAsync(Stream source, Stream destination, long maximumBytes, CancellationToken cancellationToken)
    {
        var buffer = new byte[81_920];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) return total;
            total += read;
            if (total > maximumBytes) throw new InvalidDataException($"POHODA mServer vrátil odpověď větší než povolených {maximumBytes} bajtů.");
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static string Limit(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
