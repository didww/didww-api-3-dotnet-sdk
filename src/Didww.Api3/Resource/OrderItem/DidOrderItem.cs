using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

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
