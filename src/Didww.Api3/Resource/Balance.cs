using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Balance : BaseResource
{
    public override string Type => "balances";

    [JsonProperty("total_balance")]
    public decimal? TotalBalance { get; set; }

    [JsonProperty("balance")]
    public decimal? BalanceAmount { get; set; }

    [JsonProperty("credit")]
    public decimal? Credit { get; set; }
}
