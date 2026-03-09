using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class QtyBasedPricing : BaseResource
{
    public override string Type => "qty_based_pricings";

    public static QtyBasedPricing Build(string id) => BaseResource.Build<QtyBasedPricing>(id);

    [JsonProperty("qty")]
    public int? Qty { get; set; }

    [JsonProperty("setup_price")]
    public decimal? SetupPrice { get; set; }

    [JsonProperty("monthly_price")]
    public decimal? MonthlyPrice { get; set; }
}
