using Radixal.BPC.Configuration;

namespace BcgHub.Api.Infrastructure;

public sealed class GoogleDriveOptions : BaseConfiguration
{
    public override string Prefix => "GoogleDrive";
    public string? CredentialsJson { get; set; }
    public string? CredentialsBase64 { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
    public string? RootFolderId { get; set; }
    public string FolderName { get; set; } = "BCG HUB";
}
