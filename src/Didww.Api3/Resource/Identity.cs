using Didww.Api3.Converter;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Identity : BaseResource
{
    public override string Type => "identities";

    public static Identity Build(string id) => BaseResource.Build<Identity>(id);

    private string? _firstName;
    [JsonProperty("first_name")]
    public string? FirstName { get => _firstName; set => SetProperty(ref _firstName, value); }

    private string? _lastName;
    [JsonProperty("last_name")]
    public string? LastName { get => _lastName; set => SetProperty(ref _lastName, value); }

    private string? _phoneNumber;
    [JsonProperty("phone_number")]
    public string? PhoneNumber { get => _phoneNumber; set => SetProperty(ref _phoneNumber, value); }

    private string? _idNumber;
    [JsonProperty("id_number")]
    public string? IdNumber { get => _idNumber; set => SetProperty(ref _idNumber, value); }

    private DateOnly? _birthDate;
    [JsonProperty("birth_date")]
    [JsonConverter(typeof(DateOnlyConverter))]
    public DateOnly? BirthDate { get => _birthDate; set => SetProperty(ref _birthDate, value); }

    private string? _companyName;
    [JsonProperty("company_name")]
    public string? CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }

    private string? _companyRegNumber;
    [JsonProperty("company_reg_number")]
    public string? CompanyRegNumber { get => _companyRegNumber; set => SetProperty(ref _companyRegNumber, value); }

    private string? _vatId;
    [JsonProperty("vat_id")]
    public string? VatId { get => _vatId; set => SetProperty(ref _vatId, value); }

    private string? _description;
    [JsonProperty("description")]
    public string? Description { get => _description; set => SetProperty(ref _description, value); }

    private string? _personalTaxId;
    [JsonProperty("personal_tax_id")]
    public string? PersonalTaxId { get => _personalTaxId; set => SetProperty(ref _personalTaxId, value); }

    private IdentityType? _identityType;
    [JsonProperty("identity_type")]
    public IdentityType? IdentityType { get => _identityType; set => SetProperty(ref _identityType, value); }

    private string? _externalReferenceId;
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    private string? _contactEmail;
    [JsonProperty("contact_email")]
    public string? ContactEmail { get => _contactEmail; set => SetProperty(ref _contactEmail, value); }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("verified")]
    public bool? Verified { get; set; }

    private Country? _country;
    [JsonProperty("country")]
    public Country? Country { get => _country; set => SetProperty(ref _country, value); }

    private Country? _birthCountry;
    [JsonProperty("birth_country")]
    public Country? BirthCountry { get => _birthCountry; set => SetProperty(ref _birthCountry, value); }

    [JsonProperty("proofs")]
    public List<Proof>? Proofs { get; set; }

    [JsonProperty("addresses")]
    public List<Address>? Addresses { get; set; }

    [JsonProperty("permanent_documents")]
    public List<PermanentSupportingDocument>? PermanentDocuments { get; set; }
}
