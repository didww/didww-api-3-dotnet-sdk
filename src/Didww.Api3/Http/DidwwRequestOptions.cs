namespace Didww.Api3.Http;

/// <summary>
/// Per-request settings read by the SDK's message handler.
/// </summary>
internal static class DidwwRequestOptions
{
    /// <summary>
    /// The token audience of the request. Absent means the JSON:API audience
    /// (with the JSON:API Accept default); an empty string sends the request
    /// without a token (public endpoints).
    /// </summary>
    internal static readonly HttpRequestOptionsKey<string> Audience = new("Didww.Api3.Audience");

    internal const string NoAudience = "";
}
