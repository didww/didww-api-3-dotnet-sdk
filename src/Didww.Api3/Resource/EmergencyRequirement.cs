using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class EmergencyRequirement : BaseResource
{
    public override string Type => "emergency_requirements";

    public static EmergencyRequirement Build(string id) => BaseResource.Build<EmergencyRequirement>(id);

    [JsonProperty("identity_type")]
    public string? IdentityType { get; set; }

    [JsonProperty("address_area_level")]
    public string? AddressAreaLevel { get; set; }

    /// <summary>Null when the country does not accept a personal identity for emergency calling.</summary>
    [JsonProperty("personal_area_level")]
    public string? PersonalAreaLevel { get; set; }

    /// <summary>Null when the country does not accept a business identity for emergency calling.</summary>
    [JsonProperty("business_area_level")]
    public string? BusinessAreaLevel { get; set; }

    /// <summary>Address fields that must be filled in (<c>area</c>). Empty when none are.</summary>
    [JsonProperty("address_mandatory_fields")]
    public List<string>? AddressMandatoryFields { get; set; }

    /// <summary>
    /// Fields that must be filled in for a personal identity, among <c>birth_date</c>,
    /// <c>country</c>, <c>id_number</c>, <c>personal_tax_id</c> and <c>contact_email</c>.
    /// Empty when none are.
    /// </summary>
    [JsonProperty("personal_mandatory_fields")]
    public List<string>? PersonalMandatoryFields { get; set; }

    /// <summary>
    /// Fields that must be filled in for a business identity, among <c>id_number</c>,
    /// <c>tax_id</c> (<see cref="Identity.VatId"/>), <c>country</c>, <c>company_reg_number</c>,
    /// <c>personal_tax_id</c>, <c>contact_email</c> and <c>birth_date</c>. Empty when none are.
    /// </summary>
    [JsonProperty("business_mandatory_fields")]
    public List<string>? BusinessMandatoryFields { get; set; }

    [JsonProperty("estimate_setup_time")]
    public string? EstimateSetupTime { get; set; }

    [JsonProperty("requirement_restriction_message")]
    public string? RequirementRestrictionMessage { get; set; }

    [JsonProperty("country")]
    public Country? Country { get; set; }

    /// <summary>Number type the requirement applies to. Read-only. (API 2026-10-22)</summary>
    [JsonProperty("number_type")]
    public NumberType? NumberType { get; set; }

    /// <summary>Resource-level meta. Contains setup_price and monthly_price, decimal strings.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }
}
