using Newtonsoft.Json;

namespace Didww.Api3.Resource.Prices;

/// <summary>
/// A row of an SMS OUT (A2P, P2P or OTP) price lookup (API 2026-10-22). The
/// price is in USD. When <see cref="RejectSms"/> is true <see cref="Price"/> is
/// null: the messages are refused, which is still an answer. The row ID is
/// derived from the row content and changes with the price, so it does not
/// track a destination over time.
/// </summary>
public class SmsOutPrice : BaseResource
{
    public override string Type => "prices";

    [JsonProperty("source_prefix")]
    public string? SourcePrefix { get; set; }

    [JsonProperty("destination_prefix")]
    public string? DestinationPrefix { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>Mobile country code.</summary>
    [JsonProperty("mcc")]
    public int? Mcc { get; set; }

    [JsonProperty("mcc_name")]
    public string? MccName { get; set; }

    /// <summary>Mobile network code.</summary>
    [JsonProperty("mnc")]
    public int? Mnc { get; set; }

    [JsonProperty("mnc_name")]
    public string? MncName { get; set; }

    [JsonProperty("reject_sms")]
    public bool RejectSms { get; set; }

    /// <summary>Price per message; null when messages are rejected.</summary>
    [JsonProperty("price")]
    public decimal? Price { get; set; }
}
