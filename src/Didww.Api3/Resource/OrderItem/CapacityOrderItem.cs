using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public class CapacityOrderItem : OrderItemBase
{
    [JsonIgnore]
    public override string ItemType => "capacity_order_items";

    [JsonProperty("capacity_pool_id")]
    public string? CapacityPoolId { get; set; }

    [JsonProperty("qty")]
    public int? Qty { get; set; }
}
