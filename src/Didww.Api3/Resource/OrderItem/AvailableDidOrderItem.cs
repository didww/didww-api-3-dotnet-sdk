using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public class AvailableDidOrderItem : DidOrderItem
{
    [JsonProperty("available_did_id")]
    public string? AvailableDidId { get; set; }
}
