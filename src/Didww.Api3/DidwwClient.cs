using System.IO.Compression;
using Didww.Api3.Converter;
using Didww.Api3.Exception;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3;

public class DidwwClient
{
    public const string ApiVersionHeader = "X-DIDWW-API-Version";
    public const string ApiVersion = "2026-04-16";

    public static readonly string SdkUserAgent =
        $"didww-dotnet-sdk/{typeof(DidwwClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";

    private readonly DidwwCredentials _credentials;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly string _baseUrl;

    private DidwwClient(Builder builder)
    {
        _credentials = builder.Credentials ?? throw new ArgumentNullException(nameof(builder.Credentials));
        _baseUrl = builder.BaseUrlOverride ?? _credentials.GetBaseUrl();

        _serializerSettings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DirtyContractResolver(),
            DateParseHandling = DateParseHandling.None
        };

        var handler = new ApiKeyHandler(_credentials.ApiKey)
        {
            InnerHandler = builder.InnerHandler ?? new HttpClientHandler()
        };

        _httpClient = new HttpClient(handler);

        if (builder.Timeout.HasValue)
            _httpClient.Timeout = builder.Timeout.Value;
    }

    public static Builder NewBuilder() => new();

    public string BaseUrl => _baseUrl;
    public DidwwCredentials Credentials => _credentials;

    // Read-only repositories
    public Repository.ReadOnlyRepository<Country> Countries() => new(_httpClient, _serializerSettings, _baseUrl, "countries");
    public Repository.ReadOnlyRepository<Region> Regions() => new(_httpClient, _serializerSettings, _baseUrl, "regions");
    public Repository.ReadOnlyRepository<City> Cities() => new(_httpClient, _serializerSettings, _baseUrl, "cities");
    public Repository.ReadOnlyRepository<Pop> Pops() => new(_httpClient, _serializerSettings, _baseUrl, "pops");
    public Repository.ReadOnlyRepository<DidGroupType> DidGroupTypes() => new(_httpClient, _serializerSettings, _baseUrl, "did_group_types");
    public Repository.ReadOnlyRepository<DidGroup> DidGroups() => new(_httpClient, _serializerSettings, _baseUrl, "did_groups");
    public Repository.ReadOnlyRepository<AvailableDid> AvailableDids() => new(_httpClient, _serializerSettings, _baseUrl, "available_dids");
    public Repository.ReadOnlyRepository<Area> Areas() => new(_httpClient, _serializerSettings, _baseUrl, "areas");
    public Repository.ReadOnlyRepository<NanpaPrefix> NanpaPrefixes() => new(_httpClient, _serializerSettings, _baseUrl, "nanpa_prefixes");
    public Repository.ReadOnlyRepository<ProofType> ProofTypes() => new(_httpClient, _serializerSettings, _baseUrl, "proof_types");
    public Repository.ReadOnlyRepository<PublicKey> PublicKeys() => new(_httpClient, _serializerSettings, _baseUrl, "public_keys");
    public Repository.ReadOnlyRepository<AddressRequirement> AddressRequirements() => new(_httpClient, _serializerSettings, _baseUrl, "address_requirements");
    public Repository.ReadOnlyRepository<EmergencyRequirement> EmergencyRequirements() => new(_httpClient, _serializerSettings, _baseUrl, "emergency_requirements");
    public Repository.ReadOnlyRepository<DidHistory> DidHistory() => new(_httpClient, _serializerSettings, _baseUrl, "did_history");
    public Repository.ReadOnlyRepository<SupportingDocumentTemplate> SupportingDocumentTemplates() => new(_httpClient, _serializerSettings, _baseUrl, "supporting_document_templates");

    // Singleton
    public Repository.BalanceRepository Balance() => new(_httpClient, _serializerSettings, _baseUrl, "balance");

    // CRUD repositories
    public Repository.Repository<Did> Dids() => new(_httpClient, _serializerSettings, _baseUrl, "dids");
    public Repository.Repository<VoiceInTrunk> VoiceInTrunks() => new(_httpClient, _serializerSettings, _baseUrl, "voice_in_trunks");
    public Repository.Repository<VoiceInTrunkGroup> VoiceInTrunkGroups() => new(_httpClient, _serializerSettings, _baseUrl, "voice_in_trunk_groups");
    public Repository.Repository<VoiceOutTrunk> VoiceOutTrunks() => new(_httpClient, _serializerSettings, _baseUrl, "voice_out_trunks");
    public Repository.Repository<VoiceOutTrunkRegenerateCredential> VoiceOutTrunkRegenerateCredentials() => new(_httpClient, _serializerSettings, _baseUrl, "voice_out_trunk_regenerate_credentials");
    public Repository.Repository<DidReservation> DidReservations() => new(_httpClient, _serializerSettings, _baseUrl, "did_reservations");
    public Repository.Repository<CapacityPool> CapacityPools() => new(_httpClient, _serializerSettings, _baseUrl, "capacity_pools");
    public Repository.Repository<SharedCapacityGroup> SharedCapacityGroups() => new(_httpClient, _serializerSettings, _baseUrl, "shared_capacity_groups");
    public Repository.Repository<Order> Orders() => new(_httpClient, _serializerSettings, _baseUrl, "orders");
    public Repository.Repository<Export> Exports() => new(_httpClient, _serializerSettings, _baseUrl, "exports");
    public Repository.Repository<Address> Addresses() => new(_httpClient, _serializerSettings, _baseUrl, "addresses");
    public Repository.Repository<AddressVerification> AddressVerifications() => new(_httpClient, _serializerSettings, _baseUrl, "address_verifications");
    public Repository.Repository<Identity> Identities() => new(_httpClient, _serializerSettings, _baseUrl, "identities");
    public Repository.Repository<EncryptedFile> EncryptedFiles() => new(_httpClient, _serializerSettings, _baseUrl, "encrypted_files");
    public Repository.Repository<PermanentSupportingDocument> PermanentSupportingDocuments() => new(_httpClient, _serializerSettings, _baseUrl, "permanent_supporting_documents");
    public Repository.Repository<Proof> Proofs() => new(_httpClient, _serializerSettings, _baseUrl, "proofs");
    public Repository.Repository<AddressRequirementValidation> AddressRequirementValidations() => new(_httpClient, _serializerSettings, _baseUrl, "address_requirement_validations");
    public Repository.Repository<EmergencyCallingService> EmergencyCallingServices() => new(_httpClient, _serializerSettings, _baseUrl, "emergency_calling_services");
    public Repository.Repository<EmergencyVerification> EmergencyVerifications() => new(_httpClient, _serializerSettings, _baseUrl, "emergency_verifications");
    public Repository.Repository<EmergencyRequirementValidation> EmergencyRequirementValidations() => new(_httpClient, _serializerSettings, _baseUrl, "emergency_requirement_validations");

    public async Task<string> UploadEncryptedFileAsync(byte[] encryptedData, string fileName,
        string fingerprint, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(fingerprint);

        var content = new MultipartFormDataContent
        {
            { new StringContent(fingerprint), "encrypted_files[encryption_fingerprint]" },
            { new StringContent(description ?? fileName), "encrypted_files[description]" },
            { new ByteArrayContent(encryptedData), "encrypted_files[file]", fileName }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/encrypted_files")
        {
            Content = content
        };
        request.Headers.Add("Accept", "application/json");

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new DidwwClientException($"Failed to upload encrypted file: HTTP {(int)response.StatusCode} {responseBody}");

        var root = JObject.Parse(responseBody);
        var idNode = root.SelectToken("data.id");
        if (idNode == null)
            throw new DidwwClientException($"Unexpected encrypted_files upload response: {responseBody}");

        return idNode.ToString();
    }

    public async Task DownloadExportAsync(Export export, string filePath)
    {
        ArgumentNullException.ThrowIfNull(export);
        ArgumentNullException.ThrowIfNull(filePath);

        var url = export.Url ?? throw new DidwwClientException("Export URL is null");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "application/octet-stream");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            throw new DidwwClientException($"Failed to download export: HTTP {(int)response.StatusCode}");

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = File.Create(filePath);
        await stream.CopyToAsync(fileStream);
    }

    public async Task DownloadAndDecompressExportAsync(Export export, string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await DownloadExportAsync(export, tempFile);
            await using var compressedStream = File.OpenRead(tempFile);
            await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            await using var fileStream = File.Create(filePath);
            await gzipStream.CopyToAsync(fileStream);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public class Builder
    {
        internal DidwwCredentials? Credentials { get; private set; }
        internal string? BaseUrlOverride { get; private set; }
        internal TimeSpan? Timeout { get; private set; }
        internal HttpMessageHandler? InnerHandler { get; private set; }

        public Builder SetCredentials(DidwwCredentials credentials)
        {
            Credentials = credentials;
            return this;
        }

        public Builder SetBaseUrl(string baseUrl)
        {
            BaseUrlOverride = baseUrl;
            return this;
        }

        public Builder SetTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        public Builder SetInnerHandler(HttpMessageHandler handler)
        {
            InnerHandler = handler;
            return this;
        }

        public DidwwClient Build() => new(this);
    }

    private class ApiKeyHandler : DelegatingHandler
    {
        private readonly string _apiKey;

        public ApiKeyHandler(string apiKey)
        {
            _apiKey = apiKey;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("Accept"))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/vnd.api+json");
            }

            if (!request.Headers.Contains("User-Agent"))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", SdkUserAgent);
            }

            if (!request.Headers.Contains(ApiVersionHeader))
            {
                request.Headers.TryAddWithoutValidation(ApiVersionHeader, ApiVersion);
            }

            var path = request.RequestUri?.AbsolutePath ?? "";
            if (!path.Contains("public_keys"))
            {
                request.Headers.TryAddWithoutValidation("Api-Key", _apiKey);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
