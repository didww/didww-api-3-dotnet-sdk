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
        set => SetProperty(ref _terminated, value);
    }

    private string? _description;
    [JsonProperty("description")]
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private int? _capacityLimit;
    [JsonProperty("capacity_limit")]
    public int? CapacityLimit
    {
        get => _capacityLimit;
        set => SetProperty(ref _capacityLimit, value);
    }

    [JsonProperty("channels_included_count")]
    public int? ChannelsIncludedCount { get; set; }

    private int? _dedicatedChannelsCount;
    [JsonProperty("dedicated_channels_count")]
    public int? DedicatedChannelsCount
    {
        get => _dedicatedChannelsCount;
        set => SetProperty(ref _dedicatedChannelsCount, value);
    }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the DID is emergency-enabled. (API 2026-04-16)
    /// </summary>
    [JsonProperty("emergency_enabled")]
    public bool? EmergencyEnabled { get; set; }

    private int? _billingCyclesCount;
    [JsonProperty("billing_cycles_count")]
    public int? BillingCyclesCount
    {
        get => _billingCyclesCount;
        set => SetProperty(ref _billingCyclesCount, value);
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
            SetProperty(ref _voiceInTrunk, value);
            _voiceInTrunkGroup = null;
            MarkDirty("voiceInTrunkGroup");
        }
    }

    private VoiceInTrunkGroup? _voiceInTrunkGroup;
    [JsonProperty("voice_in_trunk_group")]
    public VoiceInTrunkGroup? VoiceInTrunkGroup
    {
        get => _voiceInTrunkGroup;
        set
        {
            SetProperty(ref _voiceInTrunkGroup, value);
            _voiceInTrunk = null;
            MarkDirty("voiceInTrunk");
        }
    }

    private SmsTrunk? _smsTrunk;
    /// <summary>
    /// SMS trunk inbound messages for this DID are delivered to.
    /// Assigning a trunk also unassigns <see cref="SmsTrunkGroup"/>
    /// (the PATCH sends <c>sms_trunk_group: {data: null}</c>). To unassign,
    /// set to null on a built or loaded resource: the PATCH sends
    /// <c>sms_trunk: {data: null}</c> and leaves the group alone.
    /// (API 2026-10-22)
    /// </summary>
    [JsonProperty("sms_trunk")]
    public SmsTrunk? SmsTrunk
    {
        get => _smsTrunk;
        set
        {
            SetProperty(ref _smsTrunk, value);
            if (value != null)
            {
                _smsTrunkGroup = null;
                MarkDirty("smsTrunkGroup");
            }
        }
    }

    private SmsTrunkGroup? _smsTrunkGroup;
    /// <summary>
    /// SMS trunk group inbound messages for this DID are delivered to.
    /// Assigning a group also unassigns <see cref="SmsTrunk"/>
    /// (the PATCH sends <c>sms_trunk: {data: null}</c>). To unassign,
    /// set to null on a built or loaded resource: the PATCH sends
    /// <c>sms_trunk_group: {data: null}</c> and leaves the trunk alone.
    /// (API 2026-10-22)
    /// </summary>
    [JsonProperty("sms_trunk_group")]
    public SmsTrunkGroup? SmsTrunkGroup
    {
        get => _smsTrunkGroup;
        set
        {
            SetProperty(ref _smsTrunkGroup, value);
            if (value != null)
            {
                _smsTrunk = null;
                MarkDirty("smsTrunk");
            }
        }
    }

    private CapacityPool? _capacityPool;
    [JsonProperty("capacity_pool")]
    public CapacityPool? CapacityPool { get => _capacityPool; set => SetProperty(ref _capacityPool, value); }

    private SharedCapacityGroup? _sharedCapacityGroup;
    [JsonProperty("shared_capacity_group")]
    public SharedCapacityGroup? SharedCapacityGroup { get => _sharedCapacityGroup; set => SetProperty(ref _sharedCapacityGroup, value); }

    [JsonProperty("address_verification")]
    public AddressVerification? AddressVerification { get; set; }

    /// <summary>
    /// EmergencyCallingService assigned to this DID. To unassign, set to null
    /// on a built resource: the PATCH will send <c>emergency_calling_service: {data: null}</c>.
    /// (API 2026-04-16)
    /// </summary>
    private EmergencyCallingService? _emergencyCallingService;
    [JsonProperty("emergency_calling_service")]
    public EmergencyCallingService? EmergencyCallingService
    {
        get => _emergencyCallingService;
        set => SetProperty(ref _emergencyCallingService, value);
    }

    /// <summary>
    /// EmergencyVerification associated with this DID. (API 2026-04-16)
    /// </summary>
    private EmergencyVerification? _emergencyVerification;
    [JsonProperty("emergency_verification")]
    public EmergencyVerification? EmergencyVerification
    {
        get => _emergencyVerification;
        set => SetProperty(ref _emergencyVerification, value);
    }

    /// <summary>
    /// Identity associated with this DID. (API 2026-04-16)
    /// </summary>
    private Identity? _identity;
    [JsonProperty("identity")]
    public Identity? Identity
    {
        get => _identity;
        set => SetProperty(ref _identity, value);
    }
}
