using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Requirement : BaseResource
{
    public override string Type => "requirements";

    [JsonProperty("identity_type")] public IdentityType? IdentityType { get; set; }
    [JsonProperty("personal_area_level")] public AreaLevel? PersonalAreaLevel { get; set; }
    [JsonProperty("business_area_level")] public AreaLevel? BusinessAreaLevel { get; set; }
    [JsonProperty("address_area_level")] public AreaLevel? AddressAreaLevel { get; set; }
    [JsonProperty("personal_proof_qty")] public int? PersonalProofQty { get; set; }
    [JsonProperty("business_proof_qty")] public int? BusinessProofQty { get; set; }
    [JsonProperty("address_proof_qty")] public int? AddressProofQty { get; set; }
    [JsonProperty("personal_mandatory_fields")] public List<string>? PersonalMandatoryFields { get; set; }
    [JsonProperty("business_mandatory_fields")] public List<string>? BusinessMandatoryFields { get; set; }
    [JsonProperty("service_description_required")] public bool? ServiceDescriptionRequired { get; set; }
    [JsonProperty("restriction_message")] public string? RestrictionMessage { get; set; }

    [JsonProperty("country")] public Country? Country { get; set; }
    [JsonProperty("did_group_type")] public DidGroupType? DidGroupType { get; set; }
    [JsonProperty("personal_permanent_document")] public SupportingDocumentTemplate? PersonalPermanentDocument { get; set; }
    [JsonProperty("business_permanent_document")] public SupportingDocumentTemplate? BusinessPermanentDocument { get; set; }
    [JsonProperty("personal_onetime_document")] public SupportingDocumentTemplate? PersonalOnetimeDocument { get; set; }
    [JsonProperty("business_onetime_document")] public SupportingDocumentTemplate? BusinessOnetimeDocument { get; set; }
    [JsonProperty("personal_proof_types")] public List<ProofType>? PersonalProofTypes { get; set; }
    [JsonProperty("business_proof_types")] public List<ProofType>? BusinessProofTypes { get; set; }
    [JsonProperty("address_proof_types")] public List<ProofType>? AddressProofTypes { get; set; }
}
