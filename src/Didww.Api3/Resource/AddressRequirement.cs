using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class AddressRequirement : BaseResource
{
    public override string Type => "address_requirements";

    [JsonProperty("identity_type")] public IdentityType? IdentityType { get; set; }
    /// <summary>Null when the country does not accept a personal identity.</summary>
    [JsonProperty("personal_area_level")] public AreaLevel? PersonalAreaLevel { get; set; }
    /// <summary>Null when the country does not accept a business identity.</summary>
    [JsonProperty("business_area_level")] public AreaLevel? BusinessAreaLevel { get; set; }
    [JsonProperty("address_area_level")] public AreaLevel? AddressAreaLevel { get; set; }
    [JsonProperty("personal_proof_qty")] public int? PersonalProofQty { get; set; }
    [JsonProperty("business_proof_qty")] public int? BusinessProofQty { get; set; }
    [JsonProperty("address_proof_qty")] public int? AddressProofQty { get; set; }
    /// <summary>
    /// Identity fields that must be filled in for a personal identity, among
    /// <c>birth_date</c>, <c>country</c>, <c>birth_country</c>, <c>id_number</c>,
    /// <c>personal_tax_id</c> and <c>contact_email</c>. Empty when none are.
    /// </summary>
    [JsonProperty("personal_mandatory_fields")] public List<string>? PersonalMandatoryFields { get; set; }
    /// <summary>
    /// Identity fields that must be filled in for a business identity, among
    /// <c>id_number</c>, <c>tax_id</c> (<see cref="Identity.VatId"/>), <c>country</c>,
    /// <c>company_reg_number</c>, <c>personal_tax_id</c> and <c>contact_email</c>. Empty when none are.
    /// </summary>
    [JsonProperty("business_mandatory_fields")] public List<string>? BusinessMandatoryFields { get; set; }
    [JsonProperty("service_description_required")] public bool? ServiceDescriptionRequired { get; set; }
    [JsonProperty("restriction_message")] public string? RestrictionMessage { get; set; }
    /// <summary>Number type the requirement applies to. Read-only. (API 2026-10-22)</summary>
    [JsonProperty("number_type")] public NumberType? NumberType { get; set; }

    [JsonProperty("country")] public Country? Country { get; set; }
    [JsonProperty("personal_permanent_document")] public SupportingDocumentTemplate? PersonalPermanentDocument { get; set; }
    [JsonProperty("business_permanent_document")] public SupportingDocumentTemplate? BusinessPermanentDocument { get; set; }
    [JsonProperty("personal_onetime_document")] public SupportingDocumentTemplate? PersonalOnetimeDocument { get; set; }
    [JsonProperty("business_onetime_document")] public SupportingDocumentTemplate? BusinessOnetimeDocument { get; set; }
    [JsonProperty("personal_proof_types")] public List<ProofType>? PersonalProofTypes { get; set; }
    [JsonProperty("business_proof_types")] public List<ProofType>? BusinessProofTypes { get; set; }
    [JsonProperty("address_proof_types")] public List<ProofType>? AddressProofTypes { get; set; }
}
