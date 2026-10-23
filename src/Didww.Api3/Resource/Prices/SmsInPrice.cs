using Newtonsoft.Json;

namespace Didww.Api3.Resource.Prices;

/// <summary>
/// A row of an SMS IN price lookup (API 2026-10-22). The price is in USD. The
/// row ID is derived from the row content and changes with the price, so it
/// does not track a destination over time.
/// </summary>
public class SmsInPrice : BaseResource
{
    public override string Type => "prices";

    [JsonProperty("source_prefix")]
    public string? SourcePrefix { get; set; }

    [JsonProperty("source_number_length_min")]
    public int SourceNumberLengthMin { get; set; }

    [JsonProperty("source_number_length_max")]
    public int SourceNumberLengthMax { get; set; }

    [JsonProperty("destination_prefix")]
    public string? DestinationPrefix { get; set; }

    [JsonProperty("destination_number_length_min")]
    public int DestinationNumberLengthMin { get; set; }

    [JsonProperty("destination_number_length_max")]
    public int DestinationNumberLengthMax { get; set; }

    [JsonProperty("country")]
    public string? Country { get; set; }

    /// <summary>Price per message.</summary>
    [JsonProperty("price")]
    public decimal Price { get; set; }
}
