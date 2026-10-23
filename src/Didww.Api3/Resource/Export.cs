using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Export : BaseResource
{
    public override string Type => "exports";
    public static Export Build(string id) => BaseResource.Build<Export>(id);

    private ExportType? _exportType;
    [JsonProperty("export_type")]
    public ExportType? ExportType { get => _exportType; set => _exportType = MarkDirty("exportType", value); }

    [JsonProperty("url")] public string? Url { get; set; }

    private string? _callbackUrl;
    [JsonProperty("callback_url")]
    public string? CallbackUrl { get => _callbackUrl; set => _callbackUrl = MarkDirty("callbackUrl", value); }

    private CallbackMethod? _callbackMethod;
    [JsonProperty("callback_method")]
    public CallbackMethod? CallbackMethod { get => _callbackMethod; set => _callbackMethod = MarkDirty("callbackMethod", value); }

    [JsonProperty("status")] public ExportStatus? Status { get; set; }

    [JsonIgnore]
    public bool IsPending => Status == ExportStatus.Pending;

    [JsonIgnore]
    public bool IsProcessing => Status == ExportStatus.Processing;

    [JsonIgnore]
    public bool IsCompleted => Status == ExportStatus.Completed;

    private Dictionary<string, object>? _filters;
    /// <summary>
    /// Export filters. Keys accepted by API 2026-10-22:
    /// <list type="bullet">
    /// <item><description>every type: <c>from</c> (inclusive) and <c>to</c> (exclusive),
    /// formatted <c>YYYY-MM-DD HH:MM:SS</c> in UTC;</description></item>
    /// <item><description><see cref="Enums.ExportType.CdrIn"/>: <c>did_number</c>, <c>source_number</c>,
    /// <c>source_number_out</c>, <c>source_country_iso</c>;</description></item>
    /// <item><description><see cref="Enums.ExportType.CdrOut"/>: <c>voice_out_trunk.id</c>, <c>source</c>,
    /// <c>cli</c>, <c>destination</c>;</description></item>
    /// <item><description><see cref="Enums.ExportType.SmsIn"/> and <see cref="Enums.ExportType.SmsOut"/>:
    /// <c>source_address_eq</c>, <c>source_address_cont</c>, <c>destination_address_eq</c>,
    /// <c>destination_address_cont</c>, <c>status</c>, <c>sms_id</c>, <c>sms_trunk.id</c>
    /// (a string or a list of strings);</description></item>
    /// <item><description><see cref="Enums.ExportType.SmsIn"/> only: <c>trunk_type</c>
    /// (a string or a list of strings, see <see cref="ExportTrunkTypes"/>);</description></item>
    /// <item><description><see cref="Enums.ExportType.SmsOut"/> only: <c>service_type</c>
    /// (<c>a2p</c> or <c>p2p</c>) and <c>sender_id_verification.id</c> (a string or a list of strings).</description></item>
    /// </list>
    /// Exports created with earlier API versions may also return <c>year</c>, <c>month</c> and <c>day</c>.
    /// </summary>
    [JsonProperty("filters")]
    public Dictionary<string, object>? Filters { get => _filters; set => _filters = MarkDirty("filters", value); }

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private string? _externalReferenceId;
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => _externalReferenceId = MarkDirty("externalReferenceId", value); }
}

/// <summary>
/// Values of the <c>trunk_type</c> filter of an <c>sms_in</c> export. (API 2026-10-22)
/// </summary>
public static class ExportTrunkTypes
{
    public const string SmppSmsc = "SMPP SMSC";
    public const string SmppEsme = "SMPP ESME";
    public const string Email = "Email";
    public const string HttpIn = "HTTP IN";
}
