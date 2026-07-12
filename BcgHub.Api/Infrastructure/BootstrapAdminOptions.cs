using Radixal.BPC.Configuration;

namespace BcgHub.Api.Infrastructure;

public sealed class BootstrapAdminOptions : BaseConfiguration
{
    public override string Prefix => "BootstrapAdmin";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Password { get; set; } = "";
}
