using Didww.Api3.Converter;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class VoiceInTrunk : BaseResource
{
    public override string Type => "voice_in_trunks";

    public static VoiceInTrunk Build(string id) => BaseResource.Build<VoiceInTrunk>(id);

    private string? _name;
    [JsonProperty("name")]
    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private int? _priority;
    [JsonProperty("priority")]
    public int? Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
    }

    private int? _weight;
    [JsonProperty("weight")]
    public int? Weight
    {
        get => _weight;
        set => SetProperty(ref _weight, value);
    }

    private CliFormat? _cliFormat;
    [JsonProperty("cli_format")]
    public CliFormat? CliFormat
    {
        get => _cliFormat;
        set => SetProperty(ref _cliFormat, value);
    }

    private string? _cliPrefix;
    [JsonProperty("cli_prefix")]
    public string? CliPrefix
    {
        get => _cliPrefix;
        set => SetProperty(ref _cliPrefix, value);
    }

    private string? _description;
    [JsonProperty("description")]
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private int? _ringingTimeout;
    [JsonProperty("ringing_timeout")]
    public int? RingingTimeout
    {
        get => _ringingTimeout;
        set => SetProperty(ref _ringingTimeout, value);
    }

    private int? _capacityLimit;
    [JsonProperty("capacity_limit")]
    public int? CapacityLimit
    {
        get => _capacityLimit;
        set => SetProperty(ref _capacityLimit, value);
    }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    private string? _externalReferenceId;
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    private TrunkConfiguration? _configuration;
    [JsonProperty("configuration")]
    [JsonConverter(typeof(TrunkConfigurationConverter))]
    public TrunkConfiguration? Configuration
    {
        get => _configuration;
        set => SetProperty(ref _configuration, value);
    }

    private Pop? _pop;
    /// <summary>
    /// DIDWW point of presence the trunk is served from, or null to let DIDWW
    /// choose. To clear it, set it to null on a built or loaded resource: the
    /// PATCH sends <c>"pop": null</c>. (API 2026-10-22: an attribute, no
    /// longer a relationship)
    /// </summary>
    [JsonProperty("pop")]
    public Pop? Pop
    {
        get => _pop;
        set => SetProperty(ref _pop, value);
    }

    private VoiceInTrunkGroup? _voiceInTrunkGroup;
    [JsonProperty("voice_in_trunk_group")]
    public VoiceInTrunkGroup? VoiceInTrunkGroup
    {
        get => _voiceInTrunkGroup;
        set => SetProperty(ref _voiceInTrunkGroup, value);
    }
}
