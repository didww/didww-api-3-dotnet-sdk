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

    private Dictionary<string, object>? _filters;
    [JsonProperty("filters")]
    public Dictionary<string, object>? Filters { get => _filters; set => _filters = MarkDirty("filters", value); }

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }
}
