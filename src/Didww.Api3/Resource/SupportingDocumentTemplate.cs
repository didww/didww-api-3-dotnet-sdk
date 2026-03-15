using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class SupportingDocumentTemplate : BaseResource
{
    public override string Type => "supporting_document_templates";
    public static SupportingDocumentTemplate Build(string id) => BaseResource.Build<SupportingDocumentTemplate>(id);

    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("permanent")] public bool? Permanent { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
}
