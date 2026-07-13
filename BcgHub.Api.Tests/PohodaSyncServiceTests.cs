using System.Net;
using BcgHub.Api.Application;
using BcgHub.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BcgHub.Api.Tests;

public sealed class PohodaSyncServiceTests
{
    [Fact]
    public async Task UsesPersistedCheckpointWithOverlapAndAdvancesItOnlyAfterSuccess()
    {
        var checkpoint = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);
        var state = new FakeStateStore(checkpoint);
        var client = new FakeMServerClient();
        var importer = new FakeImportService(new PohodaImportResult(0, 0, 1, 0, 0));
        await using var provider = Services(importer);
        var service = CreateService(provider, client, state);
        var before = DateTime.UtcNow;

        var result = await service.SyncAsync("scheduled", CancellationToken.None);

        Assert.Equal(checkpoint.AddMinutes(-2), Assert.Single(client.ChangedSince));
        Assert.Equal(1, result.UnchangedCount);
        Assert.NotNull(state.LastSuccessfulSyncUtc);
        Assert.InRange(state.LastSuccessfulSyncUtc!.Value, before, DateTime.UtcNow);
        Assert.Null(state.LastError);
    }

    [Fact]
    public async Task FailedImportKeepsPreviousCheckpointForIdempotentRetry()
    {
        var checkpoint = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);
        var state = new FakeStateStore(checkpoint);
        var client = new FakeMServerClient();
        var importer = new FakeImportService(new InvalidOperationException("Import selhal"));
        await using var provider = Services(importer);
        var service = CreateService(provider, client, state);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SyncAsync("scheduled", CancellationToken.None));

        Assert.Equal(checkpoint, state.LastSuccessfulSyncUtc);
        Assert.Equal("Import selhal", state.LastError);
    }

    private static PohodaSyncService CreateService(ServiceProvider provider, IPohodaMServerClient client, IPohodaSyncStateStore state) => new(provider.GetRequiredService<IServiceScopeFactory>(), client, state, new PohodaSyncLock(), Options.Create(new PohodaOptions { Enabled = true, BaseUrl = "http://bcg.ipodnik.com:4444", CompanyNumber = "71726462", InitialLookbackDays = 30, OverlapMinutes = 2 }), NullLogger<PohodaSyncService>.Instance);

    private static ServiceProvider Services(IPohodaOrderImportService importer)
    {
        var services = new ServiceCollection();
        services.AddScoped<IPohodaOrderImportService>(_ => importer);
        return services.BuildServiceProvider();
    }

    private sealed class FakeMServerClient : IPohodaMServerClient
    {
        public List<DateTime> ChangedSince { get; } = [];

        public Task<PohodaMServerResponse> DownloadChangedOrdersAsync(DateTime changedSinceUtc, string runId, CancellationToken cancellationToken)
        {
            ChangedSince.Add(changedSinceUtc);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(new PohodaMServerResponse(response, new MemoryStream("<responsePack state=\"ok\" />"u8.ToArray()), 27));
        }
    }

    private sealed class FakeImportService : IPohodaOrderImportService
    {
        private readonly PohodaImportResult? result;
        private readonly Exception? exception;

        public FakeImportService(PohodaImportResult result) => this.result = result;
        public FakeImportService(Exception exception) => this.exception = exception;
        public Task<PohodaImportPreview> PreviewAsync(Stream xml, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PohodaImportResult> ImportAsync(Stream xml, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PohodaImportResult> ImportMServerResponseAsync(Stream xml, string companyNumber, CancellationToken cancellationToken) => exception is null ? Task.FromResult(result!) : Task.FromException<PohodaImportResult>(exception);
    }

    private sealed class FakeStateStore(DateTime? checkpoint) : IPohodaSyncStateStore
    {
        public DateTime? LastSuccessfulSyncUtc { get; private set; } = checkpoint;
        public string? LastError { get; private set; }

        public Task<PohodaSyncStateSnapshot> GetAsync(CancellationToken cancellationToken) => Task.FromResult(new PohodaSyncStateSnapshot(null, null, LastSuccessfulSyncUtc, null, null, LastError, 0, 0, 0, 0, 0));
        public Task RecordAttemptAsync(string runId, string trigger, DateTime startedAtUtc, CancellationToken cancellationToken) { LastError = null; return Task.CompletedTask; }
        public Task RecordSuccessAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, PohodaImportResult result, CancellationToken cancellationToken) { LastSuccessfulSyncUtc = startedAtUtc; LastError = null; return Task.CompletedTask; }
        public Task RecordFailureAsync(string runId, string trigger, DateTime startedAtUtc, DateTime completedAtUtc, Exception exception, CancellationToken cancellationToken) { LastError = exception.Message; return Task.CompletedTask; }
    }
}
