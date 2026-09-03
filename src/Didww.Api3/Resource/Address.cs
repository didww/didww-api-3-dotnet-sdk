using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Address : BaseResource
{
    public override string Type => "addresses";

    public static Address Build(string id) => BaseResource.Build<Address>(id);

    private string? _cityName;
    [JsonProperty("city_name")]
    public string? CityName { get => _cityName; set => SetProperty(ref _cityName, value); }

    private string? _postalCode;
    [JsonProperty("postal_code")]
    public string? PostalCode { get => _postalCode; set => SetProperty(ref _postalCode, value); }

    private string? _address;
    [JsonProperty("address")]
    public string? AddressLine { get => _address; set => SetProperty(ref _address, value, "address"); }

    private string? _description;
    [JsonProperty("description")]
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("verified")]
    public bool? Verified { get; set; }

    private string? _externalReferenceId;
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    private Country? _country;
    [JsonProperty("country")]
    public Country? Country { get => _country; set => SetProperty(ref _country, value); }

    private Identity? _identity;
    [JsonProperty("identity")]
    public Identity? Identity { get => _identity; set => SetProperty(ref _identity, value); }

    [JsonProperty("proofs")]
    public List<Proof>? Proofs { get; set; }

    private Area? _area;
    [JsonProperty("area")]
    public Area? Area { get => _area; set => SetProperty(ref _area, value); }

    private City? _city;
    [JsonProperty("city")]
    public City? City { get => _city; set => SetProperty(ref _city, value); }
}
