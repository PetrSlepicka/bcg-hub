namespace BcgHub.Api.Infrastructure;

public static class CorsOriginPolicy
{
    public static bool IsAllowed(string origin, IReadOnlyCollection<string> configuredOrigins)
    {
        if (configuredOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1" || uri.Host == "::1" || uri.Host == "[::1]";
    }
}
