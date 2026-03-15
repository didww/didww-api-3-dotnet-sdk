using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class PermanentSupportingDocument : BaseResource
{
    public override string Type => "permanent_supporting_documents";
    public static PermanentSupportingDocument Build(string id) => BaseResource.Build<PermanentSupportingDocument>(id);

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private Identity? _identity;
    [JsonProperty("identity")]
    public Identity? Identity { get => _identity; set => _identity = MarkDirty("identity", value); }

    private SupportingDocumentTemplate? _template;
    [JsonProperty("template")]
    public SupportingDocumentTemplate? Template { get => _template; set => _template = MarkDirty("template", value); }

    private List<EncryptedFile>? _files;
    [JsonProperty("files")]
    public List<EncryptedFile>? Files { get => _files; set => _files = MarkDirty("files", value); }
}
