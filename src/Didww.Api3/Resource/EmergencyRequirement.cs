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

    [JsonProperty("address_mandatory_fields")]
    public List<string>? AddressMandatoryFields { get; set; }

    [JsonProperty("personal_mandatory_fields")]
    public List<string>? PersonalMandatoryFields { get; set; }

    [JsonProperty("business_mandatory_fields")]
    public List<string>? BusinessMandatoryFields { get; set; }

    [JsonProperty("estimate_setup_time")]
    public string? EstimateSetupTime { get; set; }

    [JsonProperty("requirement_restriction_message")]
    public string? RequirementRestrictionMessage { get; set; }

    [JsonProperty("country")]
    public Country? Country { get; set; }

    [JsonProperty("did_group_type")]
    public DidGroupType? DidGroupType { get; set; }

    /// <summary>Resource-level meta. Contains setup_price and monthly_price, decimal strings.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }
}
