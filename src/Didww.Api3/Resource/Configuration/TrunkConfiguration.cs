using Didww.Api3.Internal;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Configuration;

public abstract class TrunkConfiguration
{
    [JsonIgnore]
    public abstract string ConfigurationType { get; }
}

/// <summary>
/// SIP trunk configuration. Plain data class — server-enforced multi-field
/// invariants (API 2026-04-16) are applied at serialization time by
/// <see cref="SipConfigurationJsonConverter"/>, not by setter side-effects.
/// Setting <c>EnabledSipRegistration = true</c> emits <c>"host":null</c> /
/// <c>"port":null</c> on the wire; setting <c>Host</c> to a non-empty value
/// forces <c>enabled_sip_registration</c> and <c>use_did_in_ruri</c> to
/// false on the wire. Read-only credential fields
/// (<see cref="IncomingAuthUsername"/> / <see cref="IncomingAuthPassword"/>)
/// are stripped from POST/PATCH bodies by the same converter.
/// </summary>
[JsonConverter(typeof(SipConfigurationJsonConverter))]
public class SipConfiguration : TrunkConfiguration
{
    [JsonIgnore]
    public override string ConfigurationType => "sip_configurations";

    [JsonProperty("username")]
    public string? Username { get; set; }

    [JsonProperty("host")]
    public string? Host { get; set; }

    [JsonProperty("port")]
    public int? Port { get; set; }

    /// <summary>
    /// Codecs offered on calls to the trunk, in priority order: the first has
    /// the highest priority. Left out on create, the trunk offers
    /// <c>pcmu</c>, <c>pcma</c>, <c>g729</c>, <c>g723</c> and <c>telephone_event</c>.
    /// (API 2026-10-22: replaces <c>codec_ids</c>)
    /// </summary>
    [JsonProperty("codecs")]
    public List<Codec>? Codecs { get; set; }

    /// <summary>ICE on calls to the trunk. Defaults to <see cref="Enums.IceMode.Disabled"/>. (API 2026-10-22)</summary>
    [JsonProperty("ice_mode")]
    public IceMode? IceMode { get; set; }

    /// <summary>RTP and RTCP on one port on calls to the trunk. Defaults to <see cref="Enums.RtcpMuxMode.Disabled"/>. (API 2026-10-22)</summary>
    [JsonProperty("rtcp_mux_mode")]
    public RtcpMuxMode? RtcpMuxMode { get; set; }

    /// <summary>RTCP feedback (AVPF) on calls to the trunk. Defaults to <see cref="Enums.RtcpFeedbackMode.Disabled"/>. (API 2026-10-22)</summary>
    [JsonProperty("rtcp_feedback_mode")]
    public RtcpFeedbackMode? RtcpFeedbackMode { get; set; }

    [JsonProperty("rx_dtmf_format_id")]
    public RxDtmfFormat? RxDtmfFormatId { get; set; }

    [JsonProperty("tx_dtmf_format_id")]
    public TxDtmfFormat? TxDtmfFormatId { get; set; }

    [JsonProperty("resolve_ruri")]
    public bool? ResolveRuri { get; set; }

    [JsonProperty("auth_enabled")]
    public bool? AuthEnabled { get; set; }

    [JsonProperty("auth_user")]
    public string? AuthUser { get; set; }

    [JsonProperty("auth_password")]
    public string? AuthPassword { get; set; }

    [JsonProperty("auth_from_user")]
    public string? AuthFromUser { get; set; }

    [JsonProperty("auth_from_domain")]
    public string? AuthFromDomain { get; set; }

    [JsonProperty("sst_enabled")]
    public bool? SstEnabled { get; set; }

    [JsonProperty("sst_min_timer")]
    public int? SstMinTimer { get; set; }

    [JsonProperty("sst_max_timer")]
    public int? SstMaxTimer { get; set; }

    [JsonProperty("sst_accept_501")]
    public bool? SstAccept501 { get; set; }

    [JsonProperty("sst_session_expires")]
    public int? SstSessionExpires { get; set; }

    [JsonProperty("sst_refresh_method_id")]
    public SstRefreshMethod? SstRefreshMethodId { get; set; }

    [JsonProperty("sip_timer_b")]
    public int? SipTimerB { get; set; }

    [JsonProperty("dns_srv_failover_timer")]
    public int? DnsSrvFailoverTimer { get; set; }

    [JsonProperty("rtp_ping")]
    public bool? RtpPing { get; set; }

    [JsonProperty("force_symmetric_rtp")]
    public bool? ForceSymmetricRtp { get; set; }

    [JsonProperty("rerouting_disconnect_code_ids")]
    public List<ReroutingDisconnectCode>? ReroutingDisconnectCodeIds { get; set; }

    [JsonProperty("transport_protocol_id")]
    public TransportProtocol? TransportProtocolId { get; set; }

    [JsonProperty("media_encryption_mode")]
    public MediaEncryptionMode? MediaEncryptionMode { get; set; }

    [JsonProperty("stir_shaken_mode")]
    public StirShakenMode? StirShakenMode { get; set; }

    [JsonProperty("max_transfers")]
    public int? MaxTransfers { get; set; }

    [JsonProperty("max_30x_redirects")]
    public int? Max30xRedirects { get; set; }

    [JsonProperty("allowed_rtp_ips")]
    public List<string>? AllowedRtpIps { get; set; }

    [JsonProperty("diversion_relay_policy")]
    public DiversionRelayPolicy? DiversionRelayPolicy { get; set; }

    [JsonProperty("diversion_inject_mode")]
    public DiversionInjectMode? DiversionInjectMode { get; set; }

    [JsonProperty("network_protocol_priority")]
    public NetworkProtocolPriority? NetworkProtocolPriority { get; set; }

    /// <summary>
    /// Whether SIP registration is enabled. When <c>true</c> the server
    /// generates <see cref="IncomingAuthUsername"/> /
    /// <see cref="IncomingAuthPassword"/> and the trunk's
    /// <see cref="Host"/> and <see cref="Port"/> must be left blank.
    /// (API 2026-04-16)
    /// </summary>
    [JsonProperty("enabled_sip_registration")]
    public bool? EnabledSipRegistration { get; set; }

    [JsonProperty("use_did_in_ruri")]
    public bool? UseDidInRuri { get; set; }

    [JsonProperty("cnam_lookup")]
    public bool? CnamLookup { get; set; }

    /// <summary>
    /// Server-generated SIP authentication username, returned in responses
    /// when <see cref="EnabledSipRegistration"/> is <c>true</c>.
    /// Read-only: the API rejects any write attempt with HTTP 400 "Param not allowed".
    /// (API 2026-04-16)
    /// </summary>
    [JsonProperty("incoming_auth_username")]
    public string? IncomingAuthUsername { get; set; }

    /// <summary>
    /// Server-generated SIP authentication password, returned in responses
    /// when <see cref="EnabledSipRegistration"/> is <c>true</c>.
    /// Read-only: the API rejects any write attempt with HTTP 400 "Param not allowed".
    /// (API 2026-04-16)
    /// </summary>
    [JsonProperty("incoming_auth_password")]
    public string? IncomingAuthPassword { get; set; }

    // Default ToString redacts credentials so logs / debugger / unhandled
    // exception traces never leak them. Wire payload is unaffected — the
    // converter writes the real values (and strips read-only ones).
    public override string ToString()
    {
        return $"SipConfiguration(Username={Username ?? "null"}, Host={Host ?? "null"}, Port={Port?.ToString() ?? "null"}, " +
               $"AuthPassword={Redact.Mask(AuthPassword)}, EnabledSipRegistration={EnabledSipRegistration?.ToString() ?? "null"}, " +
               $"IncomingAuthUsername={Redact.Mask(IncomingAuthUsername)}, IncomingAuthPassword={Redact.Mask(IncomingAuthPassword)})";
    }
}

public class PstnConfiguration : TrunkConfiguration
{
    [JsonIgnore]
    public override string ConfigurationType => "pstn_configurations";

    [JsonProperty("dst")]
    public string? Dst { get; set; }
}
