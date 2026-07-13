namespace BcgHub.Api.Infrastructure;

public sealed class PohodaOptions
{
    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "http://bcg.ipodnik.com:4444";
    public string CompanyNumber { get; init; } = "71726462";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public int IntervalMinutes { get; init; } = 5;
    public int InitialLookbackDays { get; init; } = 3650;
    public int OverlapMinutes { get; init; } = 2;
    public int RequestTimeoutMinutes { get; init; } = 10;
    public string TimeZoneId { get; init; } = "Europe/Prague";
}
