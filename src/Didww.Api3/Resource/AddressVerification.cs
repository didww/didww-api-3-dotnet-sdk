using Didww.Api3.Converter;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class AddressVerification : BaseResource
{
    public override string Type => "address_verifications";

    public static AddressVerification Build(string id) => BaseResource.Build<AddressVerification>(id);

    private string? _serviceDescription;
    [JsonProperty("service_description")]
    public string? ServiceDescription { get => _serviceDescription; set => _serviceDescription = MarkDirty("serviceDescription", value); }

    private string? _callbackUrl;
    [JsonProperty("callback_url")]
    public string? CallbackUrl { get => _callbackUrl; set => _callbackUrl = MarkDirty("callbackUrl", value); }

    private CallbackMethod? _callbackMethod;
    [JsonProperty("callback_method")]
    public CallbackMethod? CallbackMethod { get => _callbackMethod; set => _callbackMethod = MarkDirty("callbackMethod", value); }

    [JsonProperty("status")]
    public AddressVerificationStatus? Status { get; set; }

    [JsonProperty("reject_reasons")]
    [JsonConverter(typeof(SemicolonSplitConverter))]
    public string[]? RejectReasons { get; set; }

    [JsonProperty("reference")]
    public string? Reference { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    private Address? _address;
    [JsonProperty("address")]
    public Address? Address { get => _address; set => _address = MarkDirty("address", value); }

    private List<Did>? _dids;
    [JsonProperty("dids")]
    public List<Did>? Dids { get => _dids; set => _dids = MarkDirty("dids", value); }
}
