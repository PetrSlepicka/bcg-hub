namespace BcgHub.Api.Infrastructure;

public sealed class MicrosoftGraphOptions
{
    public string TenantId { get; set; } = "organizations";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
