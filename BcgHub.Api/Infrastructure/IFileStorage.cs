namespace BcgHub.Api.Infrastructure;

public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);
    Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken);
}
