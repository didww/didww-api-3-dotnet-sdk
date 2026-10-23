using System.Net;
using System.Net.Http.Headers;
using Didww.Api3.Auth;

namespace Didww.Api3.Http;

/// <summary>
/// Authenticates every request with a bearer token for its audience (see
/// <see cref="DidwwRequestOptions.Audience"/>). The API key is never sent here:
/// it goes to the token service only.
/// </summary>
/// <remarks>
/// A 401 answer evicts the refused token and the request is sent once more
/// with a newly minted one, when its content can be sent again (no content,
/// in-memory content, or multipart content made of those). Otherwise the 401
/// is returned as is. A 403 is never retried, and neither is a 401 from a
/// redirect the inner handler followed: the token is not sent to the redirect
/// target. The token request, the export download and the prices calls report
/// such a redirect as an error; JSON:API and upload calls return the answer of
/// the redirect target. Token errors
/// (<see cref="Didww.Api3.Exception.DidwwAuthException"/>) and transport errors propagate unchanged.
/// </remarks>
internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly TokenProvider _tokens;

    internal BearerTokenHandler(TokenProvider tokens, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var hasAudience = request.Options.TryGetValue(DidwwRequestOptions.Audience, out var audienceOption);
        PrepareRequest(request, jsonApi: !hasAudience);

        string? audience;
        if (hasAudience)
            audience = audienceOption;
        else if (IsPublicEndpoint(request.RequestUri))
            audience = null;
        else
            audience = TokenAudience.Api;
        if (string.IsNullOrEmpty(audience))
        {
            request.Headers.Authorization = null;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var token = await _tokens.GetTokenAsync(audience, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var sentUri = request.RequestUri;
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized || !Equals(request.RequestUri, sentUri))
            return response;

        await _tokens.InvalidateAsync(audience, token.AccessToken, cancellationToken).ConfigureAwait(false);
        if (!IsReplayable(request.Content))
            return response;
        response.Dispose();

        token = await _tokens.GetTokenAsync(audience, cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await _tokens.InvalidateAsync(audience, token.AccessToken, cancellationToken).ConfigureAwait(false);
        return response;
    }

    // Sets the headers common to every request. If-None-Match is never sent:
    // the services would ignore If-Modified-Since next to it.
    private static void PrepareRequest(HttpRequestMessage request, bool jsonApi)
    {
        if (request.Headers.UserAgent.Count == 0)
            request.Headers.TryAddWithoutValidation("User-Agent", DidwwClient.SdkUserAgent);
        request.Headers.Remove("If-None-Match");
        if (jsonApi && request.Headers.Accept.Count == 0)
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DidwwHttp.JsonApiMediaType));
    }

    // Whether the JSON:API path names a public resource (the segment after /v3/),
    // which is called without a token.
    internal static bool IsPublicEndpoint(Uri? uri)
    {
        if (uri == null || !uri.IsAbsoluteUri)
            return false;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var v3 = Array.IndexOf(segments, "v3");
        return v3 >= 0 && v3 + 1 < segments.Length && segments[v3 + 1] == "public_keys";
    }

    internal static bool IsReplayable(HttpContent? content) => content switch
    {
        null => true,
        ByteArrayContent => true,
        ReadOnlyMemoryContent => true,
        MultipartContent multipart => multipart.All(IsReplayable),
        _ => false
    };
}
