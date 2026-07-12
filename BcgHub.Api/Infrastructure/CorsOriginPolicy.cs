namespace BcgHub.Api.Infrastructure;

public static class CorsOriginPolicy
{
    public static bool IsAllowed(string origin, IReadOnlyCollection<string> configuredOrigins)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        return configuredOrigins.Any(configured => string.Equals(configured.TrimEnd('/'), origin.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
    }
}
