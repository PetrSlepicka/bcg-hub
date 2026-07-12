using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Http;
using Google.Apis.Services;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace BcgHub.Api.Infrastructure;

public sealed class GoogleDriveFileStorage(GoogleDriveOptions options, ILogger<GoogleDriveFileStorage> logger) : IFileStorage
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private readonly Lazy<DriveService> drive = new(() => CreateDriveService(options));
    private string? folderId;

    public async Task<string> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var parentId = await GetFolderIdAsync(cancellationToken);
        var metadata = new DriveFile { Name = Path.GetFileName(fileName), Parents = [parentId] };
        var request = drive.Value.Files.Create(metadata, content, "application/octet-stream");
        request.Fields = "id";
        var result = await request.UploadAsync(cancellationToken);
        if (result.Exception is not null) throw result.Exception;
        logger.LogInformation("Uploaded {FileName} to Google Drive folder {FolderId}.", fileName, parentId);
        return request.ResponseBody.Id;
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var stream = new MemoryStream();
        await drive.Value.Files.Get(key).DownloadAsync(stream, cancellationToken);
        stream.Position = 0;
        return stream;
    }

    public async Task<bool> TryDeleteAsync(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        try { await drive.Value.Files.Delete(key).ExecuteAsync(cancellationToken); return true; }
        catch (Google.GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound) { return false; }
    }

    public static bool HasServiceAccountCredentials(GoogleDriveOptions options) => !string.IsNullOrWhiteSpace(options.CredentialsJson) || !string.IsNullOrWhiteSpace(options.CredentialsBase64);
    public static bool HasOAuthCredentials(GoogleDriveOptions options) => !string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret) && !string.IsNullOrWhiteSpace(options.RefreshToken);

    private async Task<string> GetFolderIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderId)) return folderId;
        var parentId = string.IsNullOrWhiteSpace(options.RootFolderId) ? "root" : options.RootFolderId;
        var request = drive.Value.Files.List();
        request.Q = $"mimeType = '{FolderMimeType}' and name = '{EscapeQueryValue(options.FolderName)}' and '{EscapeQueryValue(parentId)}' in parents and trashed = false";
        request.Fields = "files(id)";
        request.PageSize = 1;
        var existing = await request.ExecuteAsync(cancellationToken);
        folderId = existing.Files.FirstOrDefault()?.Id;
        if (!string.IsNullOrWhiteSpace(folderId)) return folderId;
        var create = drive.Value.Files.Create(new DriveFile { Name = options.FolderName, MimeType = FolderMimeType, Parents = [parentId] });
        create.Fields = "id";
        folderId = (await create.ExecuteAsync(cancellationToken)).Id;
        return folderId;
    }

    private static DriveService CreateDriveService(GoogleDriveOptions options) => new(new BaseClientService.Initializer { HttpClientInitializer = CreateCredential(options), ApplicationName = "BCG HUB" });

    private static IConfigurableHttpClientInitializer CreateCredential(GoogleDriveOptions options)
    {
        if (HasOAuthCredentials(options))
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer { ClientSecrets = new ClientSecrets { ClientId = options.ClientId, ClientSecret = options.ClientSecret }, Scopes = [DriveService.Scope.DriveFile] });
            return new UserCredential(flow, "bcg-hub", new TokenResponse { RefreshToken = options.RefreshToken });
        }
        return CredentialFactory.FromJson<ServiceAccountCredential>(ResolveCredentialsJson(options)).ToGoogleCredential().CreateScoped(DriveService.Scope.DriveFile);
    }

    private static string ResolveCredentialsJson(GoogleDriveOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CredentialsJson)) return options.CredentialsJson;
        if (!string.IsNullOrWhiteSpace(options.CredentialsBase64)) return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(options.CredentialsBase64));
        throw new InvalidOperationException("Google Drive credentials are missing.");
    }

    private static string EscapeQueryValue(string value) => value.Replace("'", "\\'");
}
