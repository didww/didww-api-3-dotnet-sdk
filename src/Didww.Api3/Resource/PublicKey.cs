using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class PublicKey : BaseResource
{
    public override string Type => "public_keys";

    [JsonProperty("key")]
    public string? Key { get; set; }
}
