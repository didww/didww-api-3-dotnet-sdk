using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public abstract class OrderItemBase
{
    [JsonIgnore]
    public abstract string ItemType { get; }
}

public class DidOrderItem : OrderItemBase
{
    [JsonIgnore]
    public override string ItemType => "did_order_items";

    [JsonProperty("sku_id")]
    public string? SkuId { get; set; }

    [JsonProperty("qty")]
    public int? Qty { get; set; }

    [JsonProperty("nrc")]
    public decimal? Nrc { get; set; }

    [JsonProperty("mrc")]
    public decimal? Mrc { get; set; }

    [JsonProperty("billed_from")]
    public string? BilledFrom { get; set; }

    [JsonProperty("billed_to")]
    public string? BilledTo { get; set; }

    [JsonProperty("prorated_mrc")]
    public bool? ProratedMrc { get; set; }

    [JsonProperty("nanpa_prefix_id")]
    public string? NanpaPrefixId { get; set; }

    [JsonProperty("billing_cycles_count")]
    public int? BillingCyclesCount { get; set; }

    [JsonProperty("did_group_id")]
    public string? DidGroupId { get; set; }
}

public class AvailableDidOrderItem : DidOrderItem
{
    [JsonProperty("available_did_id")]
    public string? AvailableDidId { get; set; }
}

public class ReservationDidOrderItem : DidOrderItem
{
    [JsonProperty("did_reservation_id")]
    public string? DidReservationId { get; set; }
}

public class CapacityOrderItem : OrderItemBase
{
    [JsonIgnore]
    public override string ItemType => "capacity_order_items";

    [JsonProperty("capacity_pool_id")]
    public string? CapacityPoolId { get; set; }

    [JsonProperty("qty")]
    public int? Qty { get; set; }
}

public class GenericOrderItem : OrderItemBase
{
    [JsonIgnore]
    public override string ItemType => "generic_order_items";

    [JsonProperty("qty")]
    public int? Qty { get; set; }
}
