using Newtonsoft.Json;

namespace Didww.Api3.Resource.Prices;

/// <summary>
/// A row of a Voice IN price lookup (API 2026-10-22). Prices and fees are in
/// USD. When <see cref="RejectCalls"/> is true the price fields are null: the
/// calls are refused, which is still an answer. The row ID names the answering
/// row of the price plan.
/// </summary>
public class VoiceInPrice : BaseResource
{
    public override string Type => "prices";

    /// <summary>Destination prefix the row applies to.</summary>
    [JsonProperty("destination")]
    public string Destination { get; set; } = "";

    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>Source country of the row; null when it applies to any source.</summary>
    [JsonProperty("src_country_name")]
    public string? SrcCountryName { get; set; }

    /// <summary>Source type, for example <c>fixed</c> or <c>cellular</c>.</summary>
    [JsonProperty("source_type")]
    public string SourceType { get; set; } = "";

    /// <summary>Rate per minute; null when calls are rejected.</summary>
    [JsonProperty("rate")]
    public decimal? Rate { get; set; }

    /// <summary>Connection fee; null when calls are rejected.</summary>
    [JsonProperty("connection_fee")]
    public decimal? ConnectionFee { get; set; }

    /// <summary>First billing interval in seconds.</summary>
    [JsonProperty("initial_interval")]
    public int InitialInterval { get; set; }

    /// <summary>Following billing intervals in seconds.</summary>
    [JsonProperty("next_interval")]
    public int NextInterval { get; set; }

    [JsonProperty("reject_calls")]
    public bool RejectCalls { get; set; }
}
