using Didww.Api3.Converter;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

/// <summary>
/// SMS trunk: how messages for the assigned DIDs are delivered, or accepted
/// for sending (API 2026-10-22). The delivery is described by
/// <see cref="Configuration"/>.
///
/// Changes made inside a configuration object are not tracked: reassign the
/// property (for example <c>trunk.Configuration = trunk.Configuration</c>) to
/// send it in the next update.
/// </summary>
public class SmsTrunk : BaseResource
{
    public override string Type => "sms_trunks";

    public static SmsTrunk Build(string id) => BaseResource.Build<SmsTrunk>(id);

    private string? _name;
    /// <summary>Name of the trunk. Required on create.</summary>
    [JsonProperty("name")]
    public string? Name { get => _name; set => SetProperty(ref _name, value); }

    private string? _externalReferenceId;
    /// <summary>Customer's own identifier for the trunk, up to 100 characters.</summary>
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    private bool? _blocked;
    /// <summary>Set to true to stop the trunk from handling messages.</summary>
    [JsonProperty("blocked")]
    public bool? Blocked { get => _blocked; set => SetProperty(ref _blocked, value); }

    private int? _priority;
    /// <summary>
    /// Routing priority within an SMS trunk group, 0 to 32767; the higher value
    /// is tried first. Defaults to 100 on the trunk types that can join a
    /// group and must stay null on <c>http_out</c>.
    /// </summary>
    [JsonProperty("priority")]
    public int? Priority { get => _priority; set => SetProperty(ref _priority, value); }

    private bool? _allowAnyDidAsSource;
    /// <summary>
    /// Allow any DID of the customer as the message source. Turning it on
    /// empties <see cref="OutgoingDids"/>, which must stay empty while it is
    /// true. Refused on the trunk types that only receive messages.
    /// </summary>
    [JsonProperty("allow_any_did_as_source")]
    public bool? AllowAnyDidAsSource { get => _allowAnyDidAsSource; set => SetProperty(ref _allowAnyDidAsSource, value); }

    /// <summary>True when the trunk handles inbound messages. Read-only.</summary>
    [JsonProperty("allow_in")]
    public bool? AllowIn { get; set; }

    /// <summary>True when the trunk can send messages. Read-only.</summary>
    [JsonProperty("allow_out")]
    public bool? AllowOut { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    private SmsTrunkConfigurationBase? _configuration;
    /// <summary>
    /// Delivery configuration. On update the type must match the trunk's; the
    /// members sent are written and the rest keep their stored values.
    /// </summary>
    [JsonProperty("configuration")]
    [JsonConverter(typeof(SmsTrunkConfigurationConverter))]
    public SmsTrunkConfigurationBase? Configuration { get => _configuration; set => SetProperty(ref _configuration, value); }

    private List<Did>? _outgoingDids;
    /// <summary>
    /// DIDs accepted as the source address of a message sent through the
    /// trunk. The list replaces the stored one; assign an empty list to clear it.
    /// </summary>
    [JsonProperty("outgoing_dids")]
    public List<Did>? OutgoingDids { get => _outgoingDids; set => SetProperty(ref _outgoingDids, value); }

    /// <summary>DIDs whose inbound messages the trunk handles. Read-only.</summary>
    [JsonProperty("incoming_dids")]
    public List<Did>? IncomingDids { get; set; }

    private SmsTrunkGroup? _smsTrunkGroup;
    /// <summary>
    /// SMS trunk group the trunk belongs to. To leave the group, set to null on
    /// a built or loaded resource: the PATCH sends <c>sms_trunk_group: {data: null}</c>.
    /// </summary>
    [JsonProperty("sms_trunk_group")]
    public SmsTrunkGroup? SmsTrunkGroup { get => _smsTrunkGroup; set => SetProperty(ref _smsTrunkGroup, value); }

    /// <summary>Sender ID verifications assigned to the trunk. Read-only.</summary>
    [JsonProperty("sender_id_verifications")]
    public List<SenderIdVerification>? SenderIdVerifications { get; set; }

    /// <summary>Resource-level meta. Contains <c>host</c> on <c>http_out</c> trunks.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>URL outbound messages are submitted to, from <see cref="Meta"/>. Only set on <c>http_out</c> trunks.</summary>
    [JsonIgnore]
    public string? Host => Meta != null && Meta.TryGetValue("host", out var host) ? host?.ToString() : null;
}
