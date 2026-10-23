using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

/// <summary>
/// Emergency calling service resource (API 2026-04-16).
/// Represents an E911/E112 calling service provisioned for a specific
/// address, country and number type. Status transitions through:
/// new, in process, active, pending update, changes required, canceled.
/// </summary>
public class EmergencyCallingService : BaseResource
{
    public override string Type => "emergency_calling_services";

    public static EmergencyCallingService Build(string id) => BaseResource.Build<EmergencyCallingService>(id);

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("reference")]
    public string? Reference { get; set; }

    [JsonProperty("status")]
    public EmergencyCallingServiceStatus? Status { get; set; }

    [JsonIgnore]
    public bool IsActive => Status == EmergencyCallingServiceStatus.Active;
    [JsonIgnore]
    public bool IsCanceled => Status == EmergencyCallingServiceStatus.Canceled;
    [JsonIgnore]
    public bool IsChangesRequired => Status == EmergencyCallingServiceStatus.ChangesRequired;
    [JsonIgnore]
    public bool IsInProcess => Status == EmergencyCallingServiceStatus.InProcess;
    [JsonIgnore]
    public bool IsNew => Status == EmergencyCallingServiceStatus.New;
    [JsonIgnore]
    public bool IsPendingUpdate => Status == EmergencyCallingServiceStatus.PendingUpdate;

    [JsonProperty("activated_at")]
    public DateTimeOffset? ActivatedAt { get; set; }

    [JsonProperty("canceled_at")]
    public DateTimeOffset? CanceledAt { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("renew_date")]
    public string? RenewDate { get; set; }

    [JsonProperty("country")]
    public Country? Country { get; set; }

    /// <summary>Number type the service is provisioned for. Read-only. (API 2026-10-22)</summary>
    [JsonProperty("number_type")]
    public NumberType? NumberType { get; set; }

    [JsonProperty("order")]
    public Order? Order { get; set; }

    [JsonProperty("address")]
    public Address? Address { get; set; }

    [JsonProperty("emergency_requirement")]
    public EmergencyRequirement? EmergencyRequirement { get; set; }

    [JsonProperty("emergency_verification")]
    public EmergencyVerification? EmergencyVerification { get; set; }

    [JsonProperty("dids")]
    public List<Did>? Dids { get; set; }

    /// <summary>Resource-level meta. Contains setup_price and monthly_price.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }
}
