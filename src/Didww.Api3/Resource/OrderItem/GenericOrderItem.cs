using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public class GenericOrderItem : OrderItemBase
{
    [JsonIgnore]
    public override string ItemType => "generic_order_items";

    [JsonProperty("qty")]
    public int? Qty { get; set; }
}
