using System.Net;
using System.Text;
using BcgHub.Api.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PohodaMServerClientTests
{
    [Fact]
    public async Task SendsIncrementalReceivedOrderRequestWithRequiredAuthentication()
    {
        var handler = new RecordingHandler();
        var factory = new FakeHttpClientFactory(handler);
        var options = Options.Create(new PohodaOptions { Enabled = true, BaseUrl = "http://bcg.ipodnik.com:4444", CompanyNumber = "71726462", Username = "sync-user", Password = "secret", TimeZoneId = "Europe/Prague" });
        var client = new PohodaMServerClient(factory, options, NullLogger<PohodaMServerClient>.Instance);

        await using var response = await client.DownloadChangedOrdersAsync(new DateTime(2026, 7, 13, 8, 15, 0, DateTimeKind.Utc), "run123", CancellationToken.None);

        Assert.Equal("http://bcg.ipodnik.com:4444/xml", handler.RequestUri?.ToString());
        Assert.Equal($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes("sync-user:secret"))}", handler.Authorization);
        Assert.Equal("BCG Hub", handler.Application);
        Assert.Equal("run123", handler.Instance);
        Assert.Contains("ico=\"71726462\"", handler.Body);
        Assert.Contains("orderType=\"receivedOrder\"", handler.Body);
        Assert.Contains("<ftr:lastChanges>2026-07-13T10:15:00</ftr:lastChanges>", handler.Body);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? Authorization { get; private set; }
        public string? Application { get; private set; }
        public string? Instance { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Authorization = request.Headers.GetValues("STW-Authorization").Single();
            Application = request.Headers.GetValues("STW-Application").Single();
            Instance = request.Headers.GetValues("STW-Instance").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<responsePack state=\"ok\" />", Encoding.UTF8, "text/xml") };
        }
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { Timeout = Timeout.InfiniteTimeSpan };
    }
}
