using Didww.Api3.Converter;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Enums;

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum OrderStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "pending")] Pending,
    [System.Runtime.Serialization.EnumMember(Value = "canceled")] Canceled,
    [System.Runtime.Serialization.EnumMember(Value = "completed")] Completed
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum AddressVerificationStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "pending")] Pending,
    [System.Runtime.Serialization.EnumMember(Value = "approved")] Approved,
    [System.Runtime.Serialization.EnumMember(Value = "rejected")] Rejected
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum AreaLevel
{
    [System.Runtime.Serialization.EnumMember(Value = "world_wide")] WorldWide,
    [System.Runtime.Serialization.EnumMember(Value = "country")] Country,
    [System.Runtime.Serialization.EnumMember(Value = "area")] Area,
    [System.Runtime.Serialization.EnumMember(Value = "city")] City
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum CallbackMethod
{
    [System.Runtime.Serialization.EnumMember(Value = "post")] Post,
    [System.Runtime.Serialization.EnumMember(Value = "get")] Get
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum CliFormat
{
    [System.Runtime.Serialization.EnumMember(Value = "raw")] Raw,
    [System.Runtime.Serialization.EnumMember(Value = "e164")] E164,
    [System.Runtime.Serialization.EnumMember(Value = "local")] Local
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum DefaultDstAction
{
    [System.Runtime.Serialization.EnumMember(Value = "allow_all")] AllowAll,
    [System.Runtime.Serialization.EnumMember(Value = "reject_all")] RejectAll
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum ExportStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "pending")] Pending,
    [System.Runtime.Serialization.EnumMember(Value = "processing")] Processing,
    [System.Runtime.Serialization.EnumMember(Value = "completed")] Completed
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum ExportType
{
    [System.Runtime.Serialization.EnumMember(Value = "cdr_in")] CdrIn,
    [System.Runtime.Serialization.EnumMember(Value = "cdr_out")] CdrOut,
    [System.Runtime.Serialization.EnumMember(Value = "sms_in")] SmsIn,
    [System.Runtime.Serialization.EnumMember(Value = "sms_out")] SmsOut
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum Feature
{
    [System.Runtime.Serialization.EnumMember(Value = "voice_in")] VoiceIn,
    [System.Runtime.Serialization.EnumMember(Value = "voice_out")] VoiceOut,
    [System.Runtime.Serialization.EnumMember(Value = "t38")] T38,
    [System.Runtime.Serialization.EnumMember(Value = "sms_in")] SmsIn,
    [System.Runtime.Serialization.EnumMember(Value = "p2p")] P2p,
    [System.Runtime.Serialization.EnumMember(Value = "a2p")] A2p,
    [System.Runtime.Serialization.EnumMember(Value = "emergency")] Emergency,
    [System.Runtime.Serialization.EnumMember(Value = "cnam_out")] CnamOut
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum IdentityType
{
    [System.Runtime.Serialization.EnumMember(Value = "personal")] Personal,
    [System.Runtime.Serialization.EnumMember(Value = "business")] Business,
    [System.Runtime.Serialization.EnumMember(Value = "any")] Any
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum DiversionRelayPolicy
{
    [System.Runtime.Serialization.EnumMember(Value = "none")] None,
    [System.Runtime.Serialization.EnumMember(Value = "as_is")] AsIs,
    [System.Runtime.Serialization.EnumMember(Value = "sip")] Sip,
    [System.Runtime.Serialization.EnumMember(Value = "tel")] Tel
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum DiversionInjectMode
{
    [System.Runtime.Serialization.EnumMember(Value = "none")] None,
    [System.Runtime.Serialization.EnumMember(Value = "did_number")] DidNumber
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum NetworkProtocolPriority
{
    [System.Runtime.Serialization.EnumMember(Value = "force_ipv4")] ForceIpv4,
    [System.Runtime.Serialization.EnumMember(Value = "force_ipv6")] ForceIpv6,
    [System.Runtime.Serialization.EnumMember(Value = "any")] Any,
    [System.Runtime.Serialization.EnumMember(Value = "prefer_ipv4")] PreferIpv4,
    [System.Runtime.Serialization.EnumMember(Value = "prefer_ipv6")] PreferIpv6
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum MediaEncryptionMode
{
    [System.Runtime.Serialization.EnumMember(Value = "disabled")] Disabled,
    [System.Runtime.Serialization.EnumMember(Value = "srtp_sdes")] SrtpSdes,
    [System.Runtime.Serialization.EnumMember(Value = "srtp_dtls")] SrtpDtls,
    [System.Runtime.Serialization.EnumMember(Value = "zrtp")] Zrtp
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum OnCliMismatchAction
{
    [System.Runtime.Serialization.EnumMember(Value = "send_original_cli")] SendOriginalCli,
    [System.Runtime.Serialization.EnumMember(Value = "reject_call")] RejectCall,
    [System.Runtime.Serialization.EnumMember(Value = "replace_cli")] ReplaceCli,
    [System.Runtime.Serialization.EnumMember(Value = "randomize_cli")] RandomizeCli
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum StirShakenMode
{
    [System.Runtime.Serialization.EnumMember(Value = "disabled")] Disabled,
    [System.Runtime.Serialization.EnumMember(Value = "original")] Original,
    [System.Runtime.Serialization.EnumMember(Value = "pai")] Pai,
    [System.Runtime.Serialization.EnumMember(Value = "original_pai")] OriginalPai,
    [System.Runtime.Serialization.EnumMember(Value = "verstat")] Verstat
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum VoiceOutTrunkStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "active")] Active,
    [System.Runtime.Serialization.EnumMember(Value = "blocked")] Blocked
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum EmergencyCallingServiceStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "active")] Active,
    [System.Runtime.Serialization.EnumMember(Value = "canceled")] Canceled,
    [System.Runtime.Serialization.EnumMember(Value = "changes_required")] ChangesRequired,
    [System.Runtime.Serialization.EnumMember(Value = "in_process")] InProcess,
    [System.Runtime.Serialization.EnumMember(Value = "new")] New,
    [System.Runtime.Serialization.EnumMember(Value = "pending_update")] PendingUpdate
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum EmergencyVerificationStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "pending")] Pending,
    [System.Runtime.Serialization.EnumMember(Value = "approved")] Approved,
    [System.Runtime.Serialization.EnumMember(Value = "rejected")] Rejected
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum DidHistoryAction
{
    [System.Runtime.Serialization.EnumMember(Value = "assigned")] Assigned,
    [System.Runtime.Serialization.EnumMember(Value = "renewed")] Renewed,
    [System.Runtime.Serialization.EnumMember(Value = "canceled")] Canceled,
    [System.Runtime.Serialization.EnumMember(Value = "removed")] Removed,
    [System.Runtime.Serialization.EnumMember(Value = "billing_cycles_count_changed")] BillingCyclesCountChanged,
    [System.Runtime.Serialization.EnumMember(Value = "restored")] Restored
}

[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum DidHistoryMethod
{
    [System.Runtime.Serialization.EnumMember(Value = "system")] System,
    [System.Runtime.Serialization.EnumMember(Value = "api2")] Api2,
    [System.Runtime.Serialization.EnumMember(Value = "api3")] Api3,
    [System.Runtime.Serialization.EnumMember(Value = "staff")] Staff,
    [System.Runtime.Serialization.EnumMember(Value = "user_panel")] UserPanel
}

/// <summary>
/// Number type of a DID group, address requirement, emergency requirement or
/// emergency calling service. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum NumberType
{
    [System.Runtime.Serialization.EnumMember(Value = "local")] Local,
    [System.Runtime.Serialization.EnumMember(Value = "national")] National,
    [System.Runtime.Serialization.EnumMember(Value = "mobile")] Mobile,
    [System.Runtime.Serialization.EnumMember(Value = "toll_free")] TollFree,
    [System.Runtime.Serialization.EnumMember(Value = "shared_cost")] SharedCost,
    [System.Runtime.Serialization.EnumMember(Value = "global")] Global
}

/// <summary>
/// DIDWW point of presence a Voice IN trunk is served from. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum Pop
{
    [System.Runtime.Serialization.EnumMember(Value = "NYC")] Nyc,
    [System.Runtime.Serialization.EnumMember(Value = "FRA")] Fra,
    [System.Runtime.Serialization.EnumMember(Value = "LAC")] Lac,
    [System.Runtime.Serialization.EnumMember(Value = "MIA")] Mia,
    [System.Runtime.Serialization.EnumMember(Value = "SG")] Sg,
    [System.Runtime.Serialization.EnumMember(Value = "HK")] Hk,
    [System.Runtime.Serialization.EnumMember(Value = "AMS")] Ams
}

/// <summary>
/// Lifecycle state of a sender ID verification. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum SenderIdVerificationStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "active")] Active,
    [System.Runtime.Serialization.EnumMember(Value = "changes_required")] ChangesRequired,
    [System.Runtime.Serialization.EnumMember(Value = "closed")] Closed,
    [System.Runtime.Serialization.EnumMember(Value = "closing_in_process")] ClosingInProcess,
    [System.Runtime.Serialization.EnumMember(Value = "in_process")] InProcess,
    [System.Runtime.Serialization.EnumMember(Value = "new")] New
}

/// <summary>
/// Kind of sender a sender ID verification covers. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum SenderType
{
    [System.Runtime.Serialization.EnumMember(Value = "alphanumeric")] Alphanumeric,
    [System.Runtime.Serialization.EnumMember(Value = "carrier")] Carrier,
    [System.Runtime.Serialization.EnumMember(Value = "long_code")] LongCode,
    [System.Runtime.Serialization.EnumMember(Value = "short_code")] ShortCode,
    [System.Runtime.Serialization.EnumMember(Value = "toll_free")] TollFree
}

/// <summary>
/// HTTP method of an <c>http_in</c> SMS trunk configuration. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum SmsHttpMethod
{
    [System.Runtime.Serialization.EnumMember(Value = "get")] Get,
    [System.Runtime.Serialization.EnumMember(Value = "post")] Post,
    [System.Runtime.Serialization.EnumMember(Value = "put")] Put
}

/// <summary>
/// Body encoding of an <c>http_in</c> SMS trunk configuration. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum SmsBodyType
{
    [System.Runtime.Serialization.EnumMember(Value = "json")] Json,
    [System.Runtime.Serialization.EnumMember(Value = "html_multipart")] HtmlMultipart,
    [System.Runtime.Serialization.EnumMember(Value = "html_url_encoded")] HtmlUrlEncoded,
    [System.Runtime.Serialization.EnumMember(Value = "raw")] Raw
}

// Integer-based enums
public enum Codec
{
    TELEPHONE_EVENT = 6,
    G723 = 7,
    G729 = 8,
    PCMU = 9,
    PCMA = 10,
    SPEEX = 12,
    GSM = 13,
    G726_32 = 14,
    G721 = 15,
    G726_24 = 16,
    G726_40 = 17,
    G726_16 = 18,
    L16 = 19
}

public enum RxDtmfFormat
{
    RFC_2833 = 1,
    SIP_INFO = 2,
    RFC_2833_OR_SIP_INFO = 3
}

public enum TxDtmfFormat
{
    Disabled = 1,
    RFC_2833 = 2,
    SIP_INFO_RELAY = 3,
    SIP_INFO_DTMF = 4
}

public enum SstRefreshMethod
{
    INVITE = 1,
    UPDATE = 2,
    UPDATE_FALLBACK_INVITE = 3
}

public enum TransportProtocol
{
    UDP = 1,
    TCP = 2,
    TLS = 3
}

public enum ReroutingDisconnectCode
{
    SIP_400_BAD_REQUEST = 56,
    SIP_401_UNAUTHORIZED = 57,
    SIP_402_PAYMENT_REQUIRED = 58,
    SIP_403_FORBIDDEN = 59,
    SIP_404_NOT_FOUND = 60,
    SIP_408_REQUEST_TIMEOUT = 64,
    SIP_409_CONFLICT = 65,
    SIP_410_GONE = 66,
    SIP_412_CONDITIONAL_REQUEST_FAILED = 67,
    SIP_413_REQUEST_ENTITY_TOO_LARGE = 68,
    SIP_414_REQUEST_URI_TOO_LONG = 69,
    SIP_415_UNSUPPORTED_MEDIA_TYPE = 70,
    SIP_416_UNSUPPORTED_URI_SCHEME = 71,
    SIP_417_UNKNOWN_RESOURCE_PRIORITY = 72,
    SIP_420_BAD_EXTENSION = 73,
    SIP_421_EXTENSION_REQUIRED = 74,
    SIP_422_SESSION_INTERVAL_TOO_SMALL = 75,
    SIP_423_INTERVAL_TOO_BRIEF = 76,
    SIP_424_BAD_LOCATION_INFORMATION = 77,
    SIP_428_USE_IDENTITY_HEADER = 78,
    SIP_429_PROVIDE_REFERRER_IDENTITY = 79,
    SIP_433_ANONYMITY_DISALLOWED = 80,
    SIP_436_BAD_IDENTITY_INFO = 81,
    SIP_437_UNSUPPORTED_CERTIFICATE = 82,
    SIP_438_INVALID_IDENTITY_HEADER = 83,
    SIP_480_TEMPORARILY_UNAVAILABLE = 84,
    SIP_482_LOOP_DETECTED = 86,
    SIP_483_TOO_MANY_HOPS = 87,
    SIP_484_ADDRESS_INCOMPLETE = 88,
    SIP_485_AMBIGUOUS = 89,
    SIP_486_BUSY_HERE = 90,
    SIP_487_REQUEST_TERMINATED = 91,
    SIP_488_NOT_ACCEPTABLE_HERE = 92,
    SIP_494_SECURITY_AGREEMENT_REQUIRED = 96,
    SIP_500_SERVER_INTERNAL_ERROR = 97,
    SIP_501_NOT_IMPLEMENTED = 98,
    SIP_502_BAD_GATEWAY = 99,
    SIP_503_SERVICE_UNAVAILABLE = 100,
    SIP_504_SERVER_TIME_OUT = 101,
    SIP_505_VERSION_NOT_SUPPORTED = 102,
    SIP_513_MESSAGE_TOO_LARGE = 103,
    SIP_580_PRECONDITION_FAILURE = 104,
    SIP_600_BUSY_EVERYWHERE = 105,
    SIP_603_DECLINE = 106,
    SIP_604_DOES_NOT_EXIST_ANYWHERE = 107,
    SIP_606_NOT_ACCEPTABLE = 108,
    RINGING_TIMEOUT = 1505
}
