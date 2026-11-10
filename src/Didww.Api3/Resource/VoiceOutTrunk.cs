using Didww.Api3.Converter;
using Didww.Api3.Resource.Configuration.AuthenticationMethod;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class VoiceOutTrunk : BaseResource
{
    public override string Type => "voice_out_trunks";

    public static VoiceOutTrunk Build(string id) => BaseResource.Build<VoiceOutTrunk>(id);

    private string? _name;
    [JsonProperty("name")]
    public string? Name { get => _name; set => SetProperty(ref _name, value); }

    private OnCliMismatchAction? _onCliMismatchAction;
    [JsonProperty("on_cli_mismatch_action")]
    public OnCliMismatchAction? OnCliMismatchAction { get => _onCliMismatchAction; set => SetProperty(ref _onCliMismatchAction, value); }

    private List<string>? _allowedRtpIps;
    [JsonProperty("allowed_rtp_ips")]
    public List<string>? AllowedRtpIps { get => _allowedRtpIps; set => SetProperty(ref _allowedRtpIps, value); }

    private bool? _allowAnyDidAsCli;
    [JsonProperty("allow_any_did_as_cli")]
    public bool? AllowAnyDidAsCli { get => _allowAnyDidAsCli; set => SetProperty(ref _allowAnyDidAsCli, value); }

    [JsonProperty("status")]
    public VoiceOutTrunkStatus? Status { get; set; }

    [JsonIgnore]
    public bool IsActive => Status == VoiceOutTrunkStatus.Active;

    [JsonIgnore]
    public bool IsBlocked => Status == VoiceOutTrunkStatus.Blocked;

    private int? _capacityLimit;
    [JsonProperty("capacity_limit")]
    public int? CapacityLimit { get => _capacityLimit; set => SetProperty(ref _capacityLimit, value); }

    private decimal? _thresholdAmount;
    [JsonProperty("threshold_amount")]
    public decimal? ThresholdAmount { get => _thresholdAmount; set => SetProperty(ref _thresholdAmount, value); }

    private MediaEncryptionMode? _mediaEncryptionMode;
    [JsonProperty("media_encryption_mode")]
    public MediaEncryptionMode? MediaEncryptionMode { get => _mediaEncryptionMode; set => SetProperty(ref _mediaEncryptionMode, value); }

    private DefaultDstAction? _defaultDstAction;
    [JsonProperty("default_dst_action")]
    public DefaultDstAction? DefaultDstAction { get => _defaultDstAction; set => SetProperty(ref _defaultDstAction, value); }

    private List<string>? _dstPrefixes;
    [JsonProperty("dst_prefixes")]
    public List<string>? DstPrefixes { get => _dstPrefixes; set => SetProperty(ref _dstPrefixes, value); }

    private List<VoiceOutCodec>? _codecs;
    /// <summary>
    /// Codecs offered on calls from the trunk, in priority order: the first has
    /// the highest priority. Left out on create, the trunk offers <c>pcma</c>,
    /// <c>pcmu</c> and <c>telephone_event</c>. (API 2026-10-22)
    /// </summary>
    [JsonProperty("codecs")]
    public List<VoiceOutCodec>? Codecs { get => _codecs; set => SetProperty(ref _codecs, value); }

    private IceMode? _iceMode;
    /// <summary>ICE on calls from the trunk. Defaults to <see cref="Enums.IceMode.Accept"/>. (API 2026-10-22)</summary>
    [JsonProperty("ice_mode")]
    public IceMode? IceMode { get => _iceMode; set => SetProperty(ref _iceMode, value); }

    private RtcpMuxMode? _rtcpMuxMode;
    /// <summary>RTP and RTCP on one port on calls from the trunk. Defaults to <see cref="Enums.RtcpMuxMode.Accept"/>. (API 2026-10-22)</summary>
    [JsonProperty("rtcp_mux_mode")]
    public RtcpMuxMode? RtcpMuxMode { get => _rtcpMuxMode; set => SetProperty(ref _rtcpMuxMode, value); }

    private RtcpFeedbackMode? _rtcpFeedbackMode;
    /// <summary>RTCP feedback (AVPF) on calls from the trunk. Defaults to <see cref="Enums.RtcpFeedbackMode.Accept"/>. (API 2026-10-22)</summary>
    [JsonProperty("rtcp_feedback_mode")]
    public RtcpFeedbackMode? RtcpFeedbackMode { get => _rtcpFeedbackMode; set => SetProperty(ref _rtcpFeedbackMode, value); }

    private bool? _forceSymmetricRtp;
    [JsonProperty("force_symmetric_rtp")]
    public bool? ForceSymmetricRtp { get => _forceSymmetricRtp; set => SetProperty(ref _forceSymmetricRtp, value); }

    private bool? _rtpPing;
    [JsonProperty("rtp_ping")]
    public bool? RtpPing { get => _rtpPing; set => SetProperty(ref _rtpPing, value); }

    private string? _callbackUrl;
    [JsonProperty("callback_url")]
    public string? CallbackUrl { get => _callbackUrl; set => SetProperty(ref _callbackUrl, value); }

    [JsonProperty("threshold_reached")]
    public bool? ThresholdReached { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Customer-supplied reference. Max 100 characters. (API 2026-04-16)
    /// </summary>
    private string? _externalReferenceId;
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    /// <summary>
    /// When true, all customer DIDs assigned to this trunk are considered
    /// emergency-enabled. Cannot be combined with <see cref="EmergencyDids"/>.
    /// (API 2026-04-16)
    /// </summary>
    private bool? _emergencyEnableAll;
    [JsonProperty("emergency_enable_all")]
    public bool? EmergencyEnableAll { get => _emergencyEnableAll; set => SetProperty(ref _emergencyEnableAll, value); }

    /// <summary>
    /// Seconds of RTP inactivity before the trunk tears down the call.
    /// (API 2026-04-16)
    /// </summary>
    private int? _rtpTimeout;
    [JsonProperty("rtp_timeout")]
    public int? RtpTimeout { get => _rtpTimeout; set => SetProperty(ref _rtpTimeout, value); }

    /// <summary>
    /// Polymorphic authentication method (API 2026-04-16). One of:
    ///   - <see cref="IpOnlyAuthenticationMethod"/>: allowed_sip_ips, tech_prefix
    ///   - <see cref="CredentialsAndIpAuthenticationMethod"/>: adds server-generated username, password
    ///   - <see cref="TwilioAuthenticationMethod"/>: twilio_account_sid
    /// Unknown wire types are preserved via <see cref="GenericAuthenticationMethod"/>.
    /// Replaces the flat allowed_sip_ips / username / password attributes that
    /// existed prior to API 2026-04-16.
    /// </summary>
    private AuthenticationMethodBase? _authenticationMethod;
    [JsonProperty("authentication_method")]
    [JsonConverter(typeof(AuthenticationMethodConverter))]
    public AuthenticationMethodBase? AuthenticationMethod { get => _authenticationMethod; set => SetProperty(ref _authenticationMethod, value); }

    private List<Did>? _dids;
    [JsonProperty("dids")]
    public List<Did>? Dids { get => _dids; set => SetProperty(ref _dids, value); }

    private List<Did>? _emergencyDids;
    [JsonProperty("emergency_dids")]
    public List<Did>? EmergencyDids { get => _emergencyDids; set => SetProperty(ref _emergencyDids, value); }

    private Did? _defaultDid;
    [JsonProperty("default_did")]
    public Did? DefaultDid { get => _defaultDid; set => SetProperty(ref _defaultDid, value); }

    [JsonProperty("voice_in_trunk_group")]
    public VoiceInTrunkGroup? VoiceInTrunkGroup { get; set; }
}
