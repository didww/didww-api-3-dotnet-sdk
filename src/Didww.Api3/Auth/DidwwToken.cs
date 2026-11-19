using System.Diagnostics;
using System.Globalization;
using Didww.Api3.Internal;
using Newtonsoft.Json;
using StjJsonPropertyName = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Didww.Api3.Auth;

/// <summary>
/// An access token granted by the token service. It serializes with all fields
/// under the wire names (with Newtonsoft.Json and System.Text.Json), so that an
/// <see cref="ITokenStore"/> can persist it, while <see cref="ToString"/> and
/// the debugger display never show <see cref="AccessToken"/>.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DidwwToken
{
    /// <summary>The bearer token. Treat it like the API key.</summary>
    [JsonProperty("access_token")]
    [StjJsonPropertyName("access_token")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string AccessToken { get; set; } = "";

    [JsonProperty("token_type")]
    [StjJsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    /// <summary>The lifetime of the token in seconds.</summary>
    [JsonProperty("expires_in")]
    [StjJsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>The audiences (service hostnames) the token is valid for.</summary>
    [JsonProperty("aud")]
    [StjJsonPropertyName("aud")]
    public IReadOnlyList<string> Audience { get; set; } = Array.Empty<string>();

    [JsonProperty("scope")]
    [StjJsonPropertyName("scope")]
    public string Scope { get; set; } = "";

    [JsonProperty("api_version")]
    [StjJsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = "";

    /// <summary>When the SDK received the token.</summary>
    [JsonProperty("minted_at")]
    [StjJsonPropertyName("minted_at")]
    public DateTimeOffset MintedAt { get; set; }

    /// <summary>Describes the token without the access token.</summary>
    public override string ToString() =>
        $"DidwwToken{{AccessToken: {Redact.Mask(AccessToken)}, TokenType: {TokenType}, ExpiresIn: {ExpiresIn}, " +
        $"Audience: [{string.Join(" ", Audience ?? Array.Empty<string>())}], Scope: {Scope}, ApiVersion: {ApiVersion}, " +
        $"MintedAt: {MintedAt.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture)}}}";
}
