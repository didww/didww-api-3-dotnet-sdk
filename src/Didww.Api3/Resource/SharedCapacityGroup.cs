using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class SharedCapacityGroup : BaseResource
{
    public override string Type => "shared_capacity_groups";
    public static SharedCapacityGroup Build(string id) => BaseResource.Build<SharedCapacityGroup>(id);

    private string? _name;
    [JsonProperty("name")]
    public string? Name { get => _name; set => _name = MarkDirty("name", value); }

    private int? _sharedChannelsCount;
    [JsonProperty("shared_channels_count")]
    public int? SharedChannelsCount { get => _sharedChannelsCount; set => _sharedChannelsCount = MarkDirty("sharedChannelsCount", value); }

    private int? _meteredChannelsCount;
    [JsonProperty("metered_channels_count")]
    public int? MeteredChannelsCount { get => _meteredChannelsCount; set => _meteredChannelsCount = MarkDirty("meteredChannelsCount", value); }

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private CapacityPool? _capacityPool;
    [JsonProperty("capacity_pool")]
    public CapacityPool? CapacityPool { get => _capacityPool; set => _capacityPool = MarkDirty("capacityPool", value); }

    private List<Did>? _dids;
    [JsonProperty("dids")]
    public List<Did>? Dids { get => _dids; set => _dids = MarkDirty("dids", value); }
}
