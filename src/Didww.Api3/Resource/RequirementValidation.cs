using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class RequirementValidation : BaseResource
{
    public override string Type => "requirement_validations";
    public static RequirementValidation Build(string id) => BaseResource.Build<RequirementValidation>(id);

    [JsonProperty("result")] public bool? Result { get; set; }
    [JsonProperty("errors")] public Dictionary<string, object>? Errors { get; set; }

    private Requirement? _requirement;
    [JsonProperty("requirement")]
    public Requirement? Requirement { get => _requirement; set => _requirement = MarkDirty("requirement", value); }

    private Address? _address;
    [JsonProperty("address")]
    public Address? Address { get => _address; set => _address = MarkDirty("address", value); }

    private Identity? _identity;
    [JsonProperty("identity")]
    public Identity? Identity { get => _identity; set => _identity = MarkDirty("identity", value); }
}
