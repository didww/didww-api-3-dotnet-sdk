using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Did : BaseResource
{
    public override string Type => "dids";

    public static Did Build(string id) => BaseResource.Build<Did>(id);

    [JsonProperty("number")]
    public string? Number { get; set; }

    [JsonProperty("blocked")]
    public bool? Blocked { get; set; }

    [JsonProperty("awaiting_registration")]
    public bool? AwaitingRegistration { get; set; }

    private bool? _terminated;
    [JsonProperty("terminated")]
    public bool? Terminated
    {
        get => _terminated;
        set => _terminated = MarkDirty("terminated", value);
    }

    private string? _description;
    [JsonProperty("description")]
    public string? Description
    {
        get => _description;
        set => _description = MarkDirty("description", value);
    }

    private int? _capacityLimit;
    [JsonProperty("capacity_limit")]
    public int? CapacityLimit
    {
        get => _capacityLimit;
        set => _capacityLimit = MarkDirty("capacityLimit", value);
    }

    [JsonProperty("channels_included_count")]
    public int? ChannelsIncludedCount { get; set; }

    private int? _dedicatedChannelsCount;
    [JsonProperty("dedicated_channels_count")]
    public int? DedicatedChannelsCount
    {
        get => _dedicatedChannelsCount;
        set => _dedicatedChannelsCount = MarkDirty("dedicatedChannelsCount", value);
    }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    private int? _billingCyclesCount;
    [JsonProperty("billing_cycles_count")]
    public int? BillingCyclesCount
    {
        get => _billingCyclesCount;
        set => _billingCyclesCount = MarkDirty("billingCyclesCount", value);
    }

    [JsonProperty("order")]
    public Order? Order { get; set; }

    [JsonProperty("did_group")]
    public DidGroup? DidGroup { get; set; }

    private VoiceInTrunk? _voiceInTrunk;
    [JsonProperty("voice_in_trunk")]
    public VoiceInTrunk? VoiceInTrunk
    {
        get => _voiceInTrunk;
        set
        {
            _voiceInTrunk = MarkDirty("voiceInTrunk", value);
            _voiceInTrunkGroup = MarkDirty<VoiceInTrunkGroup?>("voiceInTrunkGroup", null);
        }
    }

    private VoiceInTrunkGroup? _voiceInTrunkGroup;
    [JsonProperty("voice_in_trunk_group")]
    public VoiceInTrunkGroup? VoiceInTrunkGroup
    {
        get => _voiceInTrunkGroup;
        set
        {
            _voiceInTrunkGroup = MarkDirty("voiceInTrunkGroup", value);
            _voiceInTrunk = MarkDirty<VoiceInTrunk?>("voiceInTrunk", null);
        }
    }

    private CapacityPool? _capacityPool;
    [JsonProperty("capacity_pool")]
    public CapacityPool? CapacityPool { get => _capacityPool; set => _capacityPool = MarkDirty("capacityPool", value); }

    private SharedCapacityGroup? _sharedCapacityGroup;
    [JsonProperty("shared_capacity_group")]
    public SharedCapacityGroup? SharedCapacityGroup { get => _sharedCapacityGroup; set => _sharedCapacityGroup = MarkDirty("sharedCapacityGroup", value); }

    [JsonProperty("address_verification")]
    public AddressVerification? AddressVerification { get; set; }
}
