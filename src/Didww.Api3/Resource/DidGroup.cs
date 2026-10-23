using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class DidGroup : BaseResource
{
    public override string Type => "did_groups";

    public static DidGroup Build(string id) => BaseResource.Build<DidGroup>(id);

    [JsonProperty("area_name")]
    public string? AreaName { get; set; }

    [JsonProperty("prefix")]
    public string? Prefix { get; set; }

    [JsonProperty("features")]
    public List<Feature>? Features { get; set; }

    [JsonProperty("is_metered")]
    public bool? IsMetered { get; set; }

    [JsonProperty("allow_additional_channels")]
    public bool? AllowAdditionalChannels { get; set; }

    [JsonProperty("service_restrictions")]
    public string? ServiceRestrictions { get; set; }

    [JsonProperty("stock_keeping_units")]
    public List<StockKeepingUnit>? StockKeepingUnits { get; set; }

    [JsonProperty("country")]
    public Country? Country { get; set; }

    [JsonProperty("region")]
    public Region? Region { get; set; }

    [JsonProperty("city")]
    public City? City { get; set; }

    /// <summary>Number type of the group. Read-only. (API 2026-10-22)</summary>
    [JsonProperty("number_type")]
    public NumberType? NumberType { get; set; }

    /// <summary>True when numbers of the group are demo numbers. Read-only. (API 2026-10-22)</summary>
    [JsonProperty("is_demo")]
    public bool? IsDemo { get; set; }

    [JsonProperty("address_requirement")]
    public AddressRequirement? AddressRequirement { get; set; }
}
