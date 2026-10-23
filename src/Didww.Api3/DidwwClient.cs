using System.Buffers;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Didww.Api3.Auth;
using Didww.Api3.Converter;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using JsonApiSerializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3;

/// <summary>
/// The DIDWW API client. It is safe for concurrent use.
/// </summary>
/// <remarks>
/// Every request is authenticated with a short-lived bearer token minted from
/// the API key by the token service; the API key itself is sent to the token
/// service only. Tokens are cached in the token store (see
/// <see cref="Builder.SetTokenStore"/>) and replaced when the API refuses them.
/// </remarks>
public class DidwwClient
{
    /// <summary>
    /// The API version requested when no other is configured. Earlier versions
    /// are not supported by this major version of the SDK.
    /// </summary>
    public const string DefaultApiVersion = "2026-10-22";

    public static readonly string SdkUserAgent =
        $"didww-dotnet-sdk/{typeof(DidwwClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";

    private const int MaxErrorBodySize = 1 << 20;
    private static readonly Regex ApiVersionPattern = new("^[0-9]{4}-[0-9]{2}-[0-9]{2}\\z", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private readonly DidwwCredentials _credentials;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly string _baseUrl;

    private DidwwClient(Builder builder)
    {
        _credentials = builder.Credentials ?? throw new ArgumentNullException(nameof(builder.Credentials));
        ValidateApiKey(_credentials.ApiKey);
        if (!Enum.IsDefined(_credentials.Environment))
        {
            throw new ArgumentException(
                $"Environment must be DidwwEnvironment.Sandbox or DidwwEnvironment.Production, got {_credentials.Environment}; " +
                "use SetBaseUrl, SetAuthUrl and the prices URL setters for other hosts");
        }

        ApiVersion = builder.ApiVersion ?? DefaultApiVersion;
        ValidateApiVersion(ApiVersion);
        if (builder.Scope.HasValue && !Enum.IsDefined(builder.Scope.Value))
            throw new ArgumentException($"Scope must be DidwwScope.Read or DidwwScope.Full, got {builder.Scope.Value}");
        Scope = builder.Scope;

        var environment = _credentials.Environment;
        _baseUrl = builder.BaseUrlOverride ?? environment.GetBaseUrl();
        AuthUrl = builder.AuthUrl ?? environment.GetAuthUrl();
        VoiceInPricesUrl = builder.VoiceInPricesUrl ?? environment.GetVoiceInPricesUrl();
        VoiceOutPricesUrl = builder.VoiceOutPricesUrl ?? environment.GetVoiceOutPricesUrl();
        SmsPricesUrl = builder.SmsPricesUrl ?? environment.GetSmsPricesUrl();
        ValidateUrl("API base", _baseUrl);
        ValidateUrl("Token service", AuthUrl);
        ValidateUrl("Voice IN prices", VoiceInPricesUrl);
        ValidateUrl("Voice OUT prices", VoiceOutPricesUrl);
        ValidateUrl("SMS prices", SmsPricesUrl);

        _serializerSettings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DirtyContractResolver(),
            DateParseHandling = DateParseHandling.None,
            // Untyped numbers (a generic SMS trunk configuration) keep their
            // exact value and scale on a read-modify-write round trip.
            FloatParseHandling = FloatParseHandling.Decimal
        };

        Logger = builder.Logger ?? NullLogger.Instance;
        TokenStore = builder.TokenStore ?? new MemoryTokenStore();
        HttpMessageHandler innerHandler = builder.InnerHandler ?? DidwwHttp.DefaultHandler.Value;
        Tokens = new TokenProvider(AuthUrl, _credentials.ApiKey, ApiVersion, Scope, TokenStore, innerHandler, Logger);

        _httpClient = new HttpClient(new BearerTokenHandler(Tokens, innerHandler), disposeHandler: false)
        {
            Timeout = builder.Timeout ?? DidwwHttp.DefaultTimeout
        };
    }

    public static Builder NewBuilder() => new();

    /// <summary>The JSON:API base URL.</summary>
    public string BaseUrl => _baseUrl;
    public DidwwCredentials Credentials => _credentials;

    /// <summary>The API version requested for access tokens.</summary>
    public string ApiVersion { get; }

    internal DidwwScope? Scope { get; }
    internal string AuthUrl { get; }
    internal string VoiceInPricesUrl { get; }
    internal string VoiceOutPricesUrl { get; }
    internal string SmsPricesUrl { get; }
    internal ITokenStore TokenStore { get; }
    internal ILogger Logger { get; }
    internal TokenProvider Tokens { get; }
    internal HttpClient HttpClient => _httpClient;
    internal JsonSerializerSettings SerializerSettings => _serializerSettings;

    // Read-only repositories
    public Repository.ReadOnlyRepository<Country> Countries() => new(_httpClient, _serializerSettings, _baseUrl, "countries");
    public Repository.ReadOnlyRepository<Region> Regions() => new(_httpClient, _serializerSettings, _baseUrl, "regions");
    public Repository.ReadOnlyRepository<City> Cities() => new(_httpClient, _serializerSettings, _baseUrl, "cities");
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
    public Repository.SingletonRepository<Balance> Balance() => new(_httpClient, _serializerSettings, _baseUrl, "balance");

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
    public Repository.Repository<SmsTrunk> SmsTrunks() => new(_httpClient, _serializerSettings, _baseUrl, "sms_trunks");
    public Repository.Repository<SmsTrunkGroup> SmsTrunkGroups() => new(_httpClient, _serializerSettings, _baseUrl, "sms_trunk_groups");
    /// <summary>Rotates the generated credentials of an SMS trunk. Only <c>CreateAsync</c> is supported.</summary>
    public Repository.Repository<SmsTrunkRegenerateCredential> SmsTrunkRegenerateCredentials() => new(_httpClient, _serializerSettings, _baseUrl, "sms_trunk_regenerate_credentials");
    /// <summary>Sender ID verifications. Only <c>ListAsync</c>, <c>FindAsync</c> and <c>UpdateAsync</c> are supported.</summary>
    public Repository.Repository<SenderIdVerification> SenderIdVerifications() => new(_httpClient, _serializerSettings, _baseUrl, "sender_id_verifications");

    // Price lists (API 2026-10-22), each on the host of its prices service.

    /// <summary>The Voice IN price list. Lookup filters: <c>number</c> (required), <c>src_country_iso</c>.</summary>
    public Prices.PricesService<Resource.Prices.VoiceInPrice> VoiceInPrices() =>
        new(_httpClient, _serializerSettings, VoiceInPricesUrl, TokenAudience.VoiceIn, "", Prices.PriceFilters.VoiceIn);

    /// <summary>The Voice OUT price list. Lookup filters: <c>number</c> (required).</summary>
    public Prices.PricesService<Resource.Prices.VoiceOutPrice> VoiceOutPrices() =>
        new(_httpClient, _serializerSettings, VoiceOutPricesUrl, TokenAudience.VoiceOut, "", Prices.PriceFilters.VoiceOut);

    /// <summary>The SMS OUT A2P price list. Lookup filters: one of <c>dst_number</c> and <c>dst_country_iso</c>, or both.</summary>
    public Prices.PricesService<Resource.Prices.SmsOutPrice> SmsOutA2PPrices() =>
        new(_httpClient, _serializerSettings, SmsPricesUrl, TokenAudience.Sms, "sms_out/a2p/", Prices.PriceFilters.Sms);

    /// <summary>The SMS OUT P2P price list. Lookup filters: one of <c>dst_number</c> and <c>dst_country_iso</c>, or both.</summary>
    public Prices.PricesService<Resource.Prices.SmsOutPrice> SmsOutP2PPrices() =>
        new(_httpClient, _serializerSettings, SmsPricesUrl, TokenAudience.Sms, "sms_out/p2p/", Prices.PriceFilters.Sms);

    /// <summary>The SMS OUT OTP price list. Lookup filters: one of <c>dst_number</c> and <c>dst_country_iso</c>, or both.</summary>
    public Prices.PricesService<Resource.Prices.SmsOutPrice> SmsOutOtpPrices() =>
        new(_httpClient, _serializerSettings, SmsPricesUrl, TokenAudience.Sms, "sms_out/otp/", Prices.PriceFilters.Sms);

    /// <summary>The SMS IN price list. Lookup filters: one of <c>dst_number</c> and <c>dst_country_iso</c>, or both.</summary>
    public Prices.PricesService<Resource.Prices.SmsInPrice> SmsInPrices() =>
        new(_httpClient, _serializerSettings, SmsPricesUrl, TokenAudience.Sms, "sms_in/", Prices.PriceFilters.Sms);

    /// <summary>
    /// Uploads one encrypted file (multipart/form-data) and returns the id of the
    /// created encrypted file. The body is built in memory, so the upload is sent
    /// once more with a new access token when the API refuses the current one.
    /// </summary>
    public async Task<string> UploadEncryptedFileAsync(byte[] encryptedData, string fileName,
        string fingerprint, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(fingerprint);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(fingerprint), "encrypted_files[encryption_fingerprint]" },
            { new StringContent(description ?? fileName), "encrypted_files[description]" },
            { new ByteArrayContent(encryptedData), "encrypted_files[file]", fileName }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/encrypted_files")
        {
            Content = content
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Options.Set(DidwwRequestOptions.Audience, TokenAudience.Api);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new DidwwClientException($"Failed to upload encrypted file: HTTP {(int)response.StatusCode} {responseBody}");

        JToken? idNode;
        try
        {
            idNode = JObject.Parse(responseBody).SelectToken("data.id");
        }
        catch (JsonException)
        {
            idNode = null;
        }
        if (idNode == null)
            throw new DidwwClientException($"Unexpected encrypted_files upload response: {responseBody}");

        return idNode.ToString();
    }

    /// <summary>
    /// Downloads the file of a completed export to <paramref name="filePath"/>
    /// unchanged (gzip-compressed CSV). The file is created only after the API
    /// answered 200. See <see cref="DownloadExportAsync(string, Stream, CancellationToken)"/>.
    /// </summary>
    public async Task DownloadExportAsync(Export export, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(export);
        ArgumentNullException.ThrowIfNull(filePath);

        var url = export.Url ?? throw new DidwwClientException("Export URL is null");
        await DownloadExportCoreAsync(url, () => File.Create(filePath), true, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads an export file from <paramref name="url"/> and writes it to
    /// <paramref name="destination"/> unchanged (gzip-compressed CSV).
    /// </summary>
    /// <remarks>
    /// The access token is sent only to the origin (scheme, host and port) of the
    /// JSON:API base URL or of the environment, as the API builds export URLs on
    /// the environment's host even when <see cref="Builder.SetBaseUrl"/> points
    /// elsewhere: a URL on any other origin raises <see cref="DidwwClientException"/>
    /// without a request. Only a 200 answer is written and redirects are not
    /// followed; any other status, including 3xx and 204, raises
    /// <see cref="DidwwApiException"/>. An exception from <paramref name="destination"/>
    /// propagates as is; a broken response body raises <see cref="DidwwClientException"/>.
    /// </remarks>
    public Task DownloadExportAsync(string url, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(destination);
        return DownloadExportCoreAsync(url, () => destination, false, cancellationToken);
    }

    /// <summary>
    /// Downloads the file of a completed export and writes the decompressed CSV
    /// to <paramref name="filePath"/>. The export URL is checked as in
    /// <see cref="DownloadExportAsync(string, Stream, CancellationToken)"/>.
    /// </summary>
    public async Task DownloadAndDecompressExportAsync(Export export, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await DownloadExportAsync(export, tempFile, cancellationToken).ConfigureAwait(false);
            var gzipStream = new GZipStream(File.OpenRead(tempFile), CompressionMode.Decompress);
            await using (gzipStream.ConfigureAwait(false))
            {
                var fileStream = File.Create(filePath);
                await using (fileStream.ConfigureAwait(false))
                {
                    await gzipStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private async Task DownloadExportCoreAsync(string url, Func<Stream> openDestination, bool ownsDestination,
        CancellationToken cancellationToken)
    {
        var uri = CheckExportUrl(url);

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Options.Set(DidwwRequestOptions.Audience, TokenAudience.Api);

        using var response = await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (DidwwHttp.WasRedirected(response, uri))
        {
            throw new DidwwClientException(
                "Export download was redirected; redirects are not followed (disable AllowAutoRedirect on the inner handler)");
        }
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorBody = Array.Empty<byte>();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_httpClient.Timeout);
            try
            {
                errorBody = await DidwwHttp.ReadLimitedAsync(response.Content, MaxErrorBodySize, timeout.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            catch (System.Exception e) when (e is IOException or HttpRequestException
                                             || (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                // The status is what matters; a broken error body is not reported instead of it.
            }
            throw UnexpectedStatusException((int)response.StatusCode, System.Text.Encoding.UTF8.GetString(errorBody));
        }

        var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (body.ConfigureAwait(false))
        {
            var destination = openDestination();
            try
            {
                await CopyExportAsync(body, destination, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (ownsDestination)
                    await destination.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task CopyExportAsync(Stream body, Stream destination, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            while (true)
            {
                int read;
                try
                {
                    read = await body.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
                catch (System.Exception e) when (e is IOException ||
                                                 (e is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    throw new DidwwClientException($"Failed to read export data: {e.Message}", e);
                }
                if (read == 0)
                    break;
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    // Refuses an export URL whose origin is neither the origin of the JSON:API
    // base URL nor that of the environment, so that the token is not sent elsewhere.
    internal Uri CheckExportUrl(string url)
    {
        var baseUri = new Uri(_baseUrl, UriKind.Absolute);
        var environmentUri = new Uri(_credentials.Environment.GetBaseUrl(), UriKind.Absolute);
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            (DidwwHttp.SameOrigin(uri, baseUri) || DidwwHttp.SameOrigin(uri, environmentUri)))
        {
            return uri;
        }

        var shown = uri != null && uri.IsAbsoluteUri ? $"{uri.Scheme}://{uri.Authority}" : url;
        throw new DidwwClientException(
            $"Export URL {shown} does not match the API base URL {baseUri.Scheme}://{baseUri.Authority} " +
            $"or the environment URL {environmentUri.Scheme}://{environmentUri.Authority}");
    }

    private static DidwwApiException UnexpectedStatusException(int status, string body)
    {
        try
        {
            if (JToken.Parse(body) is JObject root && root["errors"] is JArray errorsNode)
            {
                var errors = errorsNode.OfType<JObject>().Select(e => e.ToObject<ApiError>()!).ToList();
                if (errors.Count > 0)
                    return new DidwwApiException(status, errors);
            }
        }
        catch (JsonException)
        {
            // Not a JSON:API errors document.
        }

        return new DidwwApiException(status, new List<ApiError>
        {
            new() { Title = "Unexpected status", Detail = body.Length > 0 ? body : null }
        });
    }

    private static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required");
        if (apiKey.Any(c => (c < 0x20 && c != '\t') || c > 0x7e))
            throw new ArgumentException("API key contains characters not allowed in an HTTP header");
    }

    private static void ValidateApiVersion(string apiVersion)
    {
        if (!ApiVersionPattern.IsMatch(apiVersion))
            throw new ArgumentException($"API version must be a YYYY-MM-DD date, got \"{apiVersion}\"");
        if (string.CompareOrdinal(apiVersion, DefaultApiVersion) < 0)
        {
            throw new ArgumentException(
                $"API version {apiVersion} is not supported: this SDK requires {DefaultApiVersion} or later; " +
                "use Didww.Api3 3.x (branch release-3) for 2026-04-16");
        }
    }

    private static void ValidateUrl(string name, string url)
    {
        if (!DidwwHttp.IsHttpUrl(url))
            throw new ArgumentException($"{name} URL must be an http or https URL, got \"{url}\"");
    }

    public class Builder
    {
        internal DidwwCredentials? Credentials { get; private set; }
        internal string? BaseUrlOverride { get; private set; }
        internal TimeSpan? Timeout { get; private set; }
        internal HttpMessageHandler? InnerHandler { get; private set; }
        internal string? ApiVersion { get; private set; }
        internal DidwwScope? Scope { get; private set; }
        internal ITokenStore? TokenStore { get; private set; }
        internal ILogger? Logger { get; private set; }
        internal string? AuthUrl { get; private set; }
        internal string? VoiceInPricesUrl { get; private set; }
        internal string? VoiceOutPricesUrl { get; private set; }
        internal string? SmsPricesUrl { get; private set; }

        public Builder SetCredentials(DidwwCredentials credentials)
        {
            Credentials = credentials;
            return this;
        }

        /// <summary>
        /// Sets a custom JSON:API base URL. The token service and the price hosts
        /// are not affected, and export URLs on the environment's host are still accepted.
        /// </summary>
        public Builder SetBaseUrl(string baseUrl)
        {
            BaseUrlOverride = baseUrl;
            return this;
        }

        /// <summary>
        /// Sets the time allowed until the response headers arrive (the default is
        /// 60 seconds). JSON:API answers are small, so this bounds their whole
        /// request; file downloads are streamed and are not cut off by it.
        /// </summary>
        public Builder SetTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the inner handler, for example an <see cref="HttpClientHandler"/>
        /// with a proxy. It is used as given: set <c>AllowAutoRedirect = false</c>
        /// on it and leave automatic decompression off. A redirect it follows anyway
        /// is reported as an error afterwards by the token request, the export
        /// download and the prices calls, but by then the request, including the
        /// API key of a token request, has already reached the redirect target.
        /// JSON:API and upload calls return the answer of the redirect target,
        /// which gets no Authorization header.
        /// </summary>
        public Builder SetInnerHandler(HttpMessageHandler handler)
        {
            InnerHandler = handler;
            return this;
        }

        /// <summary>
        /// Sets the API version requested for access tokens. It must be a
        /// YYYY-MM-DD date not earlier than <see cref="DefaultApiVersion"/>;
        /// <see cref="Build"/> throws otherwise.
        /// </summary>
        public Builder SetApiVersion(string version)
        {
            ApiVersion = version;
            return this;
        }

        /// <summary>
        /// Sets the scope requested for access tokens. Without it no scope is
        /// requested and the token service grants full access.
        /// </summary>
        public Builder SetScope(DidwwScope scope)
        {
            Scope = scope;
            return this;
        }

        /// <summary>
        /// Sets the store that caches access tokens, for example one shared by
        /// several processes. The store holds live bearer tokens and must be
        /// protected accordingly. The default is a new <see cref="MemoryTokenStore"/>.
        /// </summary>
        public Builder SetTokenStore(ITokenStore store)
        {
            TokenStore = store;
            return this;
        }

        /// <summary>
        /// Sets the logger for token warnings (scope or API version granted other
        /// than requested, token store failures) and debug events. The default,
        /// like null, logs nothing. Tokens and API keys are never logged.
        /// </summary>
        public Builder SetLogger(ILogger? logger)
        {
            Logger = logger;
            return this;
        }

        /// <summary>Sets a custom token service base URL.</summary>
        public Builder SetAuthUrl(string url)
        {
            AuthUrl = url;
            return this;
        }

        /// <summary>Sets a custom Voice IN prices base URL (with or without /v3).</summary>
        public Builder SetVoiceInPricesUrl(string url)
        {
            VoiceInPricesUrl = url;
            return this;
        }

        /// <summary>Sets a custom Voice OUT prices base URL (with or without /v3).</summary>
        public Builder SetVoiceOutPricesUrl(string url)
        {
            VoiceOutPricesUrl = url;
            return this;
        }

        /// <summary>Sets a custom SMS prices base URL (with or without /v3).</summary>
        public Builder SetSmsPricesUrl(string url)
        {
            SmsPricesUrl = url;
            return this;
        }

        /// <summary>
        /// Builds the client. Throws <see cref="ArgumentNullException"/> without
        /// credentials and <see cref="ArgumentException"/> for an invalid API key,
        /// API version, scope, environment or URL.
        /// </summary>
        public DidwwClient Build() => new(this);
    }
}
