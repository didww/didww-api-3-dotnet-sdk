using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class StockKeepingUnit : BaseResource
{
    public override string Type => "stock_keeping_units";

    public static StockKeepingUnit Build(string id) => BaseResource.Build<StockKeepingUnit>(id);

    [JsonProperty("setup_price")]
    public decimal? SetupPrice { get; set; }

    [JsonProperty("monthly_price")]
    public decimal? MonthlyPrice { get; set; }

    [JsonProperty("channels_included_count")]
    public int? ChannelsIncludedCount { get; set; }
}
