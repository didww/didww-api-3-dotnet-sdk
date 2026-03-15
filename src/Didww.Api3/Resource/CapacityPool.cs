using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class CapacityPool : BaseResource
{
    public override string Type => "capacity_pools";
    public static CapacityPool Build(string id) => BaseResource.Build<CapacityPool>(id);

    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("renew_date")] public string? RenewDate { get; set; }

    private int? _totalChannelsCount;
    [JsonProperty("total_channels_count")]
    public int? TotalChannelsCount { get => _totalChannelsCount; set => _totalChannelsCount = MarkDirty("totalChannelsCount", value); }

    [JsonProperty("assigned_channels_count")] public int? AssignedChannelsCount { get; set; }
    [JsonProperty("minimum_limit")] public int? MinimumLimit { get; set; }
    [JsonProperty("minimum_qty_per_order")] public int? MinimumQtyPerOrder { get; set; }
    [JsonProperty("setup_price")] public decimal? SetupPrice { get; set; }
    [JsonProperty("monthly_price")] public decimal? MonthlyPrice { get; set; }
    [JsonProperty("metered_rate")] public decimal? MeteredRate { get; set; }

    [JsonProperty("countries")] public List<Country>? Countries { get; set; }
    [JsonProperty("shared_capacity_groups")] public List<SharedCapacityGroup>? SharedCapacityGroups { get; set; }
    [JsonProperty("qty_based_pricings")] public List<QtyBasedPricing>? QtyBasedPricings { get; set; }
}
