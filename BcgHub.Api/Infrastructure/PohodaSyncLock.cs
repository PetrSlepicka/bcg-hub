namespace BcgHub.Api.Infrastructure;

public interface IPohodaSyncLock
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken);
}

public sealed class PohodaSyncLock : IPohodaSyncLock
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken) { await semaphore.WaitAsync(cancellationToken); return new Handle(semaphore); }

    private sealed class Handle(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() { semaphore.Release(); return ValueTask.CompletedTask; }
    }
}
