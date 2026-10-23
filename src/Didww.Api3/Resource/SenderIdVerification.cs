using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

/// <summary>
/// Verification request for a sender identity used for outbound messages
/// (API 2026-10-22). Only <see cref="Name"/> and <see cref="SmsTrunk"/> can
/// be changed; verifications are listed, read and updated, but not created or
/// deleted through the API. A closed verification cannot be updated.
/// </summary>
public class SenderIdVerification : BaseResource
{
    public override string Type => "sender_id_verifications";

    public static SenderIdVerification Build(string id) => BaseResource.Build<SenderIdVerification>(id);

    private string? _name;
    [JsonProperty("name")]
    public string? Name { get => _name; set => SetProperty(ref _name, value); }

    [JsonProperty("status")]
    public SenderIdVerificationStatus? Status { get; set; }

    [JsonIgnore]
    public bool IsActive => Status == SenderIdVerificationStatus.Active;
    [JsonIgnore]
    public bool IsChangesRequired => Status == SenderIdVerificationStatus.ChangesRequired;
    [JsonIgnore]
    public bool IsClosed => Status == SenderIdVerificationStatus.Closed;
    [JsonIgnore]
    public bool IsClosingInProcess => Status == SenderIdVerificationStatus.ClosingInProcess;
    [JsonIgnore]
    public bool IsInProcess => Status == SenderIdVerificationStatus.InProcess;
    [JsonIgnore]
    public bool IsNew => Status == SenderIdVerificationStatus.New;

    [JsonProperty("sender_type")]
    public SenderType? SenderType { get; set; }

    /// <summary>
    /// Sender IDs the verification covers. For long codes and toll-free
    /// numbers they are the numbers of <see cref="Dids"/>.
    /// </summary>
    [JsonProperty("sender_ids")]
    public List<string>? SenderIds { get; set; }

    [JsonProperty("reference")]
    public string? Reference { get; set; }

    /// <summary>Reasons the last submission was rejected. Null unless the status is changes_required.</summary>
    [JsonProperty("reject_reasons")]
    public List<string>? RejectReasons { get; set; }

    /// <summary>Reviewer comment on the rejection. Null unless the status is changes_required.</summary>
    [JsonProperty("reject_comment")]
    public string? RejectComment { get; set; }

    /// <summary>Next renewal date, <c>YYYY-MM-DD</c>. Null once closed.</summary>
    [JsonProperty("renew_date")]
    public string? RenewDate { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("activated_at")]
    public DateTimeOffset? ActivatedAt { get; set; }

    [JsonProperty("closed_at")]
    public DateTimeOffset? ClosedAt { get; set; }

    private SmsTrunk? _smsTrunk;
    /// <summary>
    /// SMS trunk the verification is used on. Only <c>http_out</c>,
    /// <c>smpp_smsc</c> and <c>smpp_esme</c> trunks are accepted. To unassign,
    /// set to null on a built or loaded resource: the PATCH sends
    /// <c>sms_trunk: {data: null}</c>.
    /// </summary>
    [JsonProperty("sms_trunk")]
    public SmsTrunk? SmsTrunk { get => _smsTrunk; set => SetProperty(ref _smsTrunk, value); }

    [JsonProperty("countries")]
    public List<Country>? Countries { get; set; }

    [JsonProperty("dids")]
    public List<Did>? Dids { get; set; }

    [JsonProperty("order")]
    public Order? Order { get; set; }

    [JsonProperty("identity")]
    public Identity? Identity { get; set; }

    [JsonProperty("address")]
    public Address? Address { get; set; }

    /// <summary>Resource-level meta. Contains setup_price and monthly_price, decimal strings.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }
}
