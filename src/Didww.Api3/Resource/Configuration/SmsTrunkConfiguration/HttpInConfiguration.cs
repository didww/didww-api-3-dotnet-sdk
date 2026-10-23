using Didww.Api3.Internal;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// Inbound messages are delivered as an HTTP request. Query parameters,
/// headers and body may use the placeholders <c>{SMS_TEXT}</c>,
/// <c>{SMS_TEXT_BASE64_ENCODED}</c>, <c>{SMS_SRC_ADDR}</c>,
/// <c>{SMS_DST_ADDR}</c> and <c>{SMS_TIME}</c>.
/// <see cref="Url"/> and <see cref="HttpMethod"/> are required when the trunk
/// is created; <see cref="BodyType"/> and <see cref="Body"/> are required for
/// post and put and refused for get. (API 2026-10-22)
/// </summary>
public class HttpInConfiguration : SmsTrunkConfigurationBase
{
    [JsonIgnore]
    public override string ConfigurationType => "http_in";

    [JsonProperty("url")]
    public string? Url { get; set; }

    [JsonProperty("http_method")]
    public SmsHttpMethod? HttpMethod { get; set; }

    [JsonProperty("query_parameters")]
    public Dictionary<string, object>? QueryParameters { get; set; }

    /// <summary>Headers added to the request. They may carry credentials, so <see cref="ToString"/> redacts them.</summary>
    [JsonProperty("headers")]
    public Dictionary<string, object>? Headers { get; set; }

    [JsonProperty("body_type")]
    public SmsBodyType? BodyType { get; set; }

    /// <summary>
    /// Request body template: a JSON string for <see cref="SmsBodyType.Json"/>,
    /// an object (for example a dictionary) for the form encodings and a string
    /// for <see cref="SmsBodyType.Raw"/>. An object body is read back as a
    /// <see cref="Newtonsoft.Json.Linq.JObject"/>.
    /// </summary>
    [JsonProperty("body")]
    public object? Body { get; set; }

    [JsonProperty("follow_redirect")]
    public bool? FollowRedirect { get; set; }

    [JsonProperty("validate_https_certificate")]
    public bool? ValidateHttpsCertificate { get; set; }

    // Headers may carry credentials, so the default ToString redacts them.
    // The wire payload is unaffected.
    public override string ToString()
    {
        return $"HttpInConfiguration(Url={Url ?? "null"}, HttpMethod={HttpMethod?.ToString() ?? "null"}, " +
               $"Headers={RedactedHeaders()}, BodyType={BodyType?.ToString() ?? "null"}, " +
               $"FollowRedirect={FollowRedirect?.ToString() ?? "null"}, ValidateHttpsCertificate={ValidateHttpsCertificate?.ToString() ?? "null"})";
    }

    private string RedactedHeaders()
    {
        if (Headers == null)
            return "null";
        return Headers.Count == 0 ? "{}" : Redact.Mask("headers");
    }
}
