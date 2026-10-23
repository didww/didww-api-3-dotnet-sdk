using System.Net;

namespace Didww.Api3.Http;

/// <summary>
/// HTTP plumbing shared by the client: the default inner handler and URL helpers.
/// </summary>
internal static class DidwwHttp
{
    internal const string JsonApiMediaType = "application/vnd.api+json";

    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The inner handler of clients built without <c>SetInnerHandler</c>, shared so
    /// that they share one connection pool. It never follows redirects, keeps
    /// gzip bodies as they are and neither stores nor sends cookies, so no state
    /// is carried between clients or API keys; connecting (TCP and TLS) is
    /// bounded by 10 s.
    /// </summary>
    internal static readonly Lazy<SocketsHttpHandler> DefaultHandler = new(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.None,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(15)
    });

    /// <summary>Joins <paramref name="baseUrl"/> and <paramref name="path"/>, appending /v3 to the base when it does not end with it.</summary>
    internal static string JoinV3(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        if (!trimmed.EndsWith("/v3", StringComparison.Ordinal))
            trimmed += "/v3";
        return trimmed + "/" + path.TrimStart('/');
    }

    /// <summary>Whether <paramref name="url"/> is an absolute http or https URL with a host.</summary>
    internal static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrEmpty(uri.Host);

    /// <summary>
    /// Whether <paramref name="a"/> and <paramref name="b"/> have the same scheme,
    /// host name (ignoring case and a trailing dot) and port (the scheme's default
    /// port when none is given).
    /// </summary>
    internal static bool SameOrigin(Uri a, Uri b) =>
        string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Host.TrimEnd('.'), b.Host.TrimEnd('.'), StringComparison.OrdinalIgnoreCase) &&
        a.Port == b.Port;

    /// <summary>Whether a response came from another URL than the one sent, i.e. a redirect was followed.</summary>
    internal static bool WasRedirected(HttpResponseMessage response, Uri? sentUri) =>
        sentUri != null && response.RequestMessage?.RequestUri is { } finalUri && finalUri != sentUri;

    /// <summary>Reads at most <paramref name="limit"/> bytes of the body.</summary>
    internal static async Task<byte[]> ReadLimitedAsync(HttpContent content, int limit, CancellationToken cancellationToken)
    {
        var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            while (buffer.Length < limit)
            {
                var read = await stream.ReadAsync(chunk.AsMemory(0, (int)Math.Min(chunk.Length, limit - buffer.Length)),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                await buffer.WriteAsync(chunk.AsMemory(0, read), CancellationToken.None).ConfigureAwait(false);
            }
            return buffer.ToArray();
        }
    }
}
