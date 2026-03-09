using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class CapacityPool : BaseResource
{
    public override string Type => "capacity_pools";
    public static CapacityPool Build(string id) => BaseResource.Build<CapacityPool>(id);

    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("renew_date")] public string? RenewDate { get; set; }

    private int? _totalChannelsCount;
    [JsonProperty("total_channels_count")]
    public int? TotalChannelsCount { get => _totalChannelsCount; set => _totalChannelsCount = MarkDirty("totalChannelsCount", value); }

    [JsonProperty("assigned_channels_count")] public int? AssignedChannelsCount { get; set; }
    [JsonProperty("minimum_limit")] public int? MinimumLimit { get; set; }
    [JsonProperty("minimum_qty_per_order")] public int? MinimumQtyPerOrder { get; set; }
    [JsonProperty("setup_price")] public decimal? SetupPrice { get; set; }
    [JsonProperty("monthly_price")] public decimal? MonthlyPrice { get; set; }
    [JsonProperty("metered_rate")] public decimal? MeteredRate { get; set; }

    [JsonProperty("countries")] public List<Country>? Countries { get; set; }
    [JsonProperty("shared_capacity_groups")] public List<SharedCapacityGroup>? SharedCapacityGroups { get; set; }
    [JsonProperty("qty_based_pricings")] public List<QtyBasedPricing>? QtyBasedPricings { get; set; }
}

public class SharedCapacityGroup : BaseResource
{
    public override string Type => "shared_capacity_groups";
    public static SharedCapacityGroup Build(string id) => BaseResource.Build<SharedCapacityGroup>(id);

    private string? _name;
    [JsonProperty("name")]
    public string? Name { get => _name; set => _name = MarkDirty("name", value); }

    private int? _sharedChannelsCount;
    [JsonProperty("shared_channels_count")]
    public int? SharedChannelsCount { get => _sharedChannelsCount; set => _sharedChannelsCount = MarkDirty("sharedChannelsCount", value); }

    private int? _meteredChannelsCount;
    [JsonProperty("metered_channels_count")]
    public int? MeteredChannelsCount { get => _meteredChannelsCount; set => _meteredChannelsCount = MarkDirty("meteredChannelsCount", value); }

    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private CapacityPool? _capacityPool;
    [JsonProperty("capacity_pool")]
    public CapacityPool? CapacityPool { get => _capacityPool; set => _capacityPool = MarkDirty("capacityPool", value); }

    private List<Did>? _dids;
    [JsonProperty("dids")]
    public List<Did>? Dids { get => _dids; set => _dids = MarkDirty("dids", value); }
}

public class DidReservation : BaseResource
{
    public override string Type => "did_reservations";
    public static DidReservation Build(string id) => BaseResource.Build<DidReservation>(id);

    private string? _description;
    [JsonProperty("description")]
    public string? Description { get => _description; set => _description = MarkDirty("description", value); }

    [JsonProperty("expire_at")] public DateTimeOffset? ExpireAt { get; set; }
    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private AvailableDid? _availableDid;
    [JsonProperty("available_did")]
    public AvailableDid? AvailableDid { get => _availableDid; set => _availableDid = MarkDirty("availableDid", value); }
}

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

public class EncryptedFile : BaseResource
{
    public override string Type => "encrypted_files";
    public static EncryptedFile Build(string id) => BaseResource.Build<EncryptedFile>(id);

    [JsonProperty("description")] public string? Description { get; set; }
    [JsonProperty("expire_at")] public DateTimeOffset? ExpireAt { get; set; }
    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }
}

public class PublicKey : BaseResource
{
    public override string Type => "public_keys";

    [JsonProperty("key")]
    public string? Key { get; set; }
}

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

public class SupportingDocumentTemplate : BaseResource
{
    public override string Type => "supporting_document_templates";
    public static SupportingDocumentTemplate Build(string id) => BaseResource.Build<SupportingDocumentTemplate>(id);

    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("permanent")] public bool? Permanent { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
}
