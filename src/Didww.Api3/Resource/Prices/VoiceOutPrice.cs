using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Prices;

/// <summary>
/// A row of a Voice OUT price lookup (API 2026-10-22). Prices and fees are in
/// USD. When <see cref="RejectCalls"/> is true the price fields are null: the
/// calls are refused, which is still an answer. The <c>Next*</c> members and
/// <see cref="EffectiveAt"/> announce a scheduled price change. The row ID stays
/// the same when a re-import changes only the price.
/// </summary>
public class VoiceOutPrice : BaseResource
{
    public override string Type => "prices";

    [JsonProperty("category")]
    public VoiceOutPriceCategory? Category { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("network")]
    public string? Network { get; set; }

    /// <summary>Destination prefix the row applies to.</summary>
    [JsonProperty("prefix")]
    public string Prefix { get; set; } = "";

    /// <summary>Rate per minute; null when calls are rejected.</summary>
    [JsonProperty("rate")]
    public decimal? Rate { get; set; }

    /// <summary>Connection fee; null when calls are rejected.</summary>
    [JsonProperty("connection_fee")]
    public decimal? ConnectionFee { get; set; }

    /// <summary>First billing increment in seconds.</summary>
    [JsonProperty("initial_increment")]
    public int InitialIncrement { get; set; }

    /// <summary>Following billing increments in seconds.</summary>
    [JsonProperty("recurring_increment")]
    public int RecurringIncrement { get; set; }

    /// <summary>
    /// Rate from <see cref="EffectiveAt"/>; null when no change is scheduled, when the price is
    /// removed or when the destination is going to reject calls (see <see cref="Comment"/>).
    /// </summary>
    [JsonProperty("next_rate")]
    public decimal? NextRate { get; set; }

    /// <summary>Connection fee from <see cref="EffectiveAt"/>; null whenever <see cref="NextRate"/> is.</summary>
    [JsonProperty("next_connection_fee")]
    public decimal? NextConnectionFee { get; set; }

    [JsonProperty("next_initial_increment")]
    public int? NextInitialIncrement { get; set; }

    [JsonProperty("next_recurring_increment")]
    public int? NextRecurringIncrement { get; set; }

    /// <summary>When the scheduled change takes effect; null when none is scheduled.</summary>
    [JsonProperty("effective_at")]
    public DateTimeOffset? EffectiveAt { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("reject_calls")]
    public bool RejectCalls { get; set; }
}
