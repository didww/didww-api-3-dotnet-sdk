using System.Buffers;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Didww.Api3.Auth;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Prices;

/// <summary>
/// One price list of a prices service: its price file, a gzipped CSV, and its
/// lookup. Requests carry a token for the audience of the service host.
/// Without a price plan of the service every call fails with
/// <see cref="DidwwPricesErrorKind.PricePlanNotAssigned"/>.
/// </summary>
/// <remarks>
/// <para>
/// A failed call raises <see cref="DidwwPricesException"/> (see
/// <see cref="DidwwPricesException.Kind"/>), <see cref="DidwwAuthException"/>
/// when no token could be obtained, or <see cref="ArgumentException"/> for
/// invalid arguments. <see cref="LookupAsync"/> raises
/// <see cref="DidwwClientException"/> when a 200 answer cannot be parsed or
/// holds no prices.
/// </para>
/// <para>
/// Redirects are never followed: a 3xx answer, or a redirect followed by a
/// custom inner handler, raises <see cref="DidwwPricesErrorKind.UnexpectedStatus"/>.
/// A 503 answer to a file request whose Retry-After is at most a minute is
/// retried up to twice, after that Retry-After (at least a second) plus a small
/// random delay; any other answer, including 429, is not retried. When the
/// cancellation token fires during that wait, the
/// <see cref="OperationCanceledException"/> propagates as is.
/// </para>
/// </remarks>
/// <typeparam name="TPrice">The row type of the price list.</typeparam>
public sealed class PricesService<TPrice> where TPrice : BaseResource
{
    private const string PriceFileName = "prices.csv.gz";
    private const string PriceLookupPath = "lookup/prices";
    private const string CodePricePlanNotAssigned = "price_plan_not_assigned";
    private const string CodePriceNotFound = "price_not_found";
    private const int MaxUnavailableRetries = 2;
    private const int TempFileAttempts = 10;
    private const int MaxErrorBodySize = 1 << 20;
    private const int CopyBufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _serializerSettings;
    private readonly string _baseUrl;
    private readonly string _audience;
    private readonly string _prefix;
    private readonly PriceFilters _filters;

    internal PricesService(HttpClient httpClient, JsonSerializerSettings serializerSettings, string baseUrl,
        string audience, string prefix, PriceFilters filters)
    {
        _httpClient = httpClient;
        _serializerSettings = serializerSettings;
        _baseUrl = baseUrl;
        _audience = audience;
        _prefix = prefix;
        _filters = filters;
    }

    // Test knobs: the wait before a 503 retry and the jitter source.
    internal Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = Task.Delay;
    internal Func<double> NextDouble { get; set; } = Random.Shared.NextDouble;

    private string FilePath => _prefix + PriceFileName;
    private string LookupPath => _prefix + PriceLookupPath;

    /// <summary>
    /// Returns the prices matching <paramref name="filters"/>, such as
    /// <c>{"number": "18005550123"}</c>. Null and blank values are dropped. A
    /// missing or unknown filter raises <see cref="ArgumentException"/> before
    /// any request; the formats of the values are checked by the service
    /// (<see cref="DidwwPricesErrorKind.BadRequest"/>).
    /// </summary>
    public async Task<PriceLookup<TPrice>> LookupAsync(IReadOnlyDictionary<string, string?> filters,
        CancellationToken cancellationToken = default)
    {
        var query = _filters.BuildQuery(filters, LookupPath);
        var uri = new Uri(DidwwHttp.JoinV3(_baseUrl, LookupPath) + "?" + query);

        // The body is read after SendAsync returns; the client timeout bounds the
        // whole lookup, reading the body included.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_httpClient.Timeout);
        using var request = NewRequest(HttpMethod.Get, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DidwwHttp.JsonApiMediaType));
        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        CheckNotRedirected(response, uri);
        if (response.StatusCode != HttpStatusCode.OK)
            throw await ErrorAsync(response, cancellationToken, timeout.Token).ConfigureAwait(false);

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        catch (System.Exception e) when (IsTransportError(e, cancellationToken))
        {
            throw new DidwwPricesException(DidwwPricesErrorKind.ConnectionError, 200, innerException: e);
        }
        return ParseLookup(body);
    }

    /// <summary>
    /// Returns the metadata of the price file without its content. A given
    /// <paramref name="ifModifiedSince"/> is sent as If-Modified-Since.
    /// </summary>
    public Task<PriceFile> HeadAsync(DateTimeOffset? ifModifiedSince = null,
        CancellationToken cancellationToken = default) =>
        RequestFileAsync(HttpMethod.Head, ifModifiedSince, null, cancellationToken);

    /// <summary>
    /// Writes the price file to <paramref name="destination"/> exactly as served:
    /// gzipped, never decompressed (see <see cref="PriceFileReader"/>). A given
    /// <paramref name="ifModifiedSince"/> is sent as If-Modified-Since; a 304
    /// answer writes nothing.
    /// </summary>
    /// <remarks>
    /// Only a 200 answer is written, but <paramref name="destination"/> may hold
    /// partial data when the transfer fails. An exception of
    /// <paramref name="destination"/> propagates as is; a broken response body
    /// raises <see cref="DidwwPricesErrorKind.ConnectionError"/>.
    /// </remarks>
    public Task<PriceFile> DownloadAsync(Stream destination, DateTimeOffset? ifModifiedSince = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        return RequestFileAsync(HttpMethod.Get, ifModifiedSince, destination, cancellationToken);
    }

    /// <summary>
    /// Downloads the price file to <paramref name="path"/>. The file is written
    /// to a temporary file next to <paramref name="path"/>, which replaces it
    /// only when the download succeeds, so a failed download leaves an existing
    /// file as it was. The new file gets the default mode of a new file (0666
    /// minus the umask on Unix) and the Last-Modified of the answer as its
    /// modification time.
    /// </summary>
    /// <remarks>
    /// Exceptions of the file system (a missing directory, the temporary file,
    /// the times, the move) propagate as is. On Windows the replacement fails
    /// while <paramref name="path"/> is open without <see cref="FileShare.Delete"/>.
    /// </remarks>
    public Task<PriceFile> DownloadToFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return DownloadToFileCoreAsync(path, null, cancellationToken);
    }

    /// <summary>
    /// Downloads the price file to <paramref name="path"/> like
    /// <see cref="DownloadToFileAsync"/> unless the file at <paramref name="path"/>
    /// is current: the modification time of an existing file is sent as
    /// If-Modified-Since, and a 304 answer leaves the file untouched. It trusts
    /// the modification time of any file at <paramref name="path"/>.
    /// </summary>
    public Task<PriceFile> FetchAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        var existing = new FileInfo(path);
        DateTimeOffset? ifModifiedSince = existing.Exists
            ? new DateTimeOffset(DateTime.SpecifyKind(existing.LastWriteTimeUtc, DateTimeKind.Utc))
            : null;
        return DownloadToFileCoreAsync(path, ifModifiedSince, cancellationToken);
    }

    private async Task<PriceFile> DownloadToFileCoreAsync(string path, DateTimeOffset? ifModifiedSince,
        CancellationToken cancellationToken)
    {
        var (stream, tempPath) = CreateTempFile(path);
        try
        {
            PriceFile file;
            await using (stream.ConfigureAwait(false))
            {
                file = await RequestFileAsync(HttpMethod.Get, ifModifiedSince, stream, cancellationToken)
                    .ConfigureAwait(false);
                if (file.StatusCode == 200)
                {
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    stream.Flush(flushToDisk: true);
                }
            }

            if (file.NotModified)
                return file;
            if (file.LastModified is { } lastModified)
                File.SetLastWriteTimeUtc(tempPath, lastModified.UtcDateTime);
            File.Move(tempPath, path, overwrite: true);
            return file;
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (System.Exception)
            {
                // Already moved, or nothing more can be done.
            }
        }
    }

    // Creates a new file next to path with a random suffix. Its mode is that of
    // any new file (0666 minus the umask on Unix), not the 0600 of a temp file.
    private static (FileStream Stream, string Path) CreateTempFile(string path)
    {
        var attempt = 0;
        while (true)
        {
            attempt++;
            var tempPath = $"{path}.{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8))}.tmp";
            try
            {
                var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    bufferSize: 0, FileOptions.Asynchronous);
                return (stream, tempPath);
            }
            catch (IOException) when (attempt < TempFileAttempts && File.Exists(tempPath))
            {
                // Name taken: try another suffix.
            }
        }
    }

    // Sends a HEAD or GET for the price file, retrying a 503 whose Retry-After
    // is at most a minute up to MaxUnavailableRetries times.
    private async Task<PriceFile> RequestFileAsync(HttpMethod method, DateTimeOffset? ifModifiedSince,
        Stream? destination, CancellationToken cancellationToken)
    {
        var uri = new Uri(DidwwHttp.JoinV3(_baseUrl, FilePath));
        var attempt = 0;
        while (true)
        {
            attempt++;
            TimeSpan wait;
            using (var request = NewRequest(method, uri))
            {
                if (ifModifiedSince.HasValue)
                    request.Headers.IfModifiedSince = ifModifiedSince;
                using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);
                CheckNotRedirected(response, uri);

                var status = (int)response.StatusCode;
                if (status is 200 or 304)
                {
                    long written = 0;
                    if (status == 200 && destination != null)
                        written = await CopyAsync(response.Content, destination, cancellationToken).ConfigureAwait(false);
                    return NewPriceFile(response, written);
                }

                if (UnavailableRetryWait(response, attempt) is { } retryWait)
                {
                    wait = retryWait;
                }
                else
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeout.CancelAfter(_httpClient.Timeout);
                    throw await ErrorAsync(response, cancellationToken, timeout.Token).ConfigureAwait(false);
                }
            }
            await Delay(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    // The wait before retrying a 503 answer whose Retry-After is at most a
    // minute, or null when the answer is not retried.
    private TimeSpan? UnavailableRetryWait(HttpResponseMessage response, int attempt)
    {
        if ((int)response.StatusCode == 503 && response.Headers.RetryAfter?.Delta is { } retryAfter &&
            retryAfter >= TimeSpan.Zero && retryAfter <= PricesServiceConstants.MaxUnavailableRetryAfter &&
            attempt <= MaxUnavailableRetries)
        {
            var atLeastOneSecond = retryAfter < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : retryAfter;
            return TokenProvider.SpreadRetryAfter(atLeastOneSecond, NextDouble);
        }
        return null;
    }

    private HttpRequestMessage NewRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Options.Set(DidwwRequestOptions.Audience, _audience);
        return request;
    }

    // Sends the request, turning transport errors into a connection error. Token
    // errors and the caller's cancellation propagate as is.
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completion,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, completion, cancellationToken).ConfigureAwait(false);
        }
        catch (System.Exception e) when (IsTransportError(e, cancellationToken))
        {
            throw new DidwwPricesException(DidwwPricesErrorKind.ConnectionError, innerException: e);
        }
    }

    // A transport failure: a connection, TLS or body error, or a timeout (an
    // OperationCanceledException that the caller did not ask for).
    private static bool IsTransportError(System.Exception e, CancellationToken cancellationToken) =>
        e is HttpRequestException or IOException ||
        (e is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    // A custom inner handler may follow a redirect: the answer then comes from
    // another URL than the one sent, which is refused.
    private static void CheckNotRedirected(HttpResponseMessage response, Uri uri)
    {
        if (DidwwHttp.WasRedirected(response, uri))
        {
            throw new DidwwPricesException(DidwwPricesErrorKind.UnexpectedStatus, (int)response.StatusCode,
                innerException: new HttpRequestException(
                    "The request was redirected; redirects are not followed (disable AllowAutoRedirect on the inner handler)"));
        }
    }

    // Copies the body to destination. An exception of destination propagates as
    // is; a read error is a connection error.
    private static async Task<long> CopyAsync(HttpContent content, Stream destination,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            Stream body;
            try
            {
                body = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (System.Exception e) when (IsTransportError(e, cancellationToken))
            {
                throw new DidwwPricesException(DidwwPricesErrorKind.ConnectionError, 200, innerException: e);
            }

            await using (body.ConfigureAwait(false))
            {
                long written = 0;
                while (true)
                {
                    int read;
                    try
                    {
                        read = await body.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    }
                    catch (System.Exception e) when (IsTransportError(e, cancellationToken))
                    {
                        throw new DidwwPricesException(DidwwPricesErrorKind.ConnectionError, 200, innerException: e);
                    }
                    if (read == 0)
                        return written;
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    written += read;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static PriceFile NewPriceFile(HttpResponseMessage response, long bytesWritten)
    {
        var headers = response.Content.Headers;
        // The raw header only: the parsed ContentLength computes 0 for an empty body.
        long? contentLength = null;
        if (response.StatusCode == HttpStatusCode.OK &&
            headers.NonValidated.TryGetValues("Content-Length", out var values) &&
            values.Count == 1 &&
            long.TryParse(values.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var length))
        {
            contentLength = length;
        }

        var disposition = headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"');
        return new PriceFile((int)response.StatusCode, headers.LastModified, contentLength,
            string.IsNullOrEmpty(fileName) ? null : fileName, bytesWritten);
    }

    private PriceLookup<TPrice> ParseLookup(string body)
    {
        List<TPrice>? rows;
        try
        {
            rows = JsonConvert.DeserializeObject<List<TPrice>>(body, _serializerSettings);
        }
        catch (System.Exception e) when (e is not OutOfMemoryException)
        {
            throw new DidwwClientException($"Failed to parse price lookup: {e.Message}", e);
        }
        if (rows == null || rows.Count == 0 || rows.Contains(null!))
            throw new DidwwClientException("Failed to parse price lookup: the answer holds no prices");

        var totalRecords = 0;
        try
        {
            using var reader = new JsonTextReader(new StringReader(body)) { DateParseHandling = DateParseHandling.None };
            switch (JObject.Load(reader)["meta"])
            {
                case JObject meta when meta["total_records"] is { Type: not JTokenType.Null } total:
                    totalRecords = total.ToObject<int>();
                    break;
                case null or JObject or { Type: JTokenType.Null }:
                    break;
                default:
                    throw new JsonSerializationException("meta is not an object");
            }
        }
        catch (System.Exception e) when (e is JsonException or FormatException or OverflowException or ArgumentException)
        {
            throw new DidwwClientException($"Failed to parse price lookup meta: {e.Message}", e);
        }
        return new PriceLookup<TPrice>(rows, totalRecords);
    }

    // Maps an answer other than a success to a DidwwPricesException. The body is
    // read with readToken; only the caller's cancellationToken propagates.
    private static async Task<DidwwPricesException> ErrorAsync(HttpResponseMessage response,
        CancellationToken cancellationToken, CancellationToken readToken)
    {
        var status = (int)response.StatusCode;
        var errors = ParseErrors(await ReadErrorBodyAsync(response.Content, cancellationToken, readToken)
            .ConfigureAwait(false));
        var code = errors.Count > 0 ? errors[0].Code : null;
        var retryAfter = response.Headers.RetryAfter?.Delta is { } delta && delta >= TimeSpan.Zero ? delta : (TimeSpan?)null;

        DidwwPricesErrorKind kind;
        switch (status)
        {
            case 400:
                kind = DidwwPricesErrorKind.BadRequest;
                break;
            case 401:
                kind = DidwwPricesErrorKind.NotAuthorized;
                break;
            case 403:
                kind = DidwwPricesErrorKind.AccessDenied;
                break;
            case 404:
                if (string.IsNullOrEmpty(code))
                    code = CodePricePlanNotAssigned;
                kind = code == CodePriceNotFound
                    ? DidwwPricesErrorKind.PriceNotFound
                    : DidwwPricesErrorKind.PricePlanNotAssigned;
                break;
            case 406:
                kind = DidwwPricesErrorKind.NotAcceptable;
                break;
            case 429:
                kind = DidwwPricesErrorKind.TooManyRequests;
                break;
            case 503:
                kind = DidwwPricesErrorKind.ServiceUnavailable;
                break;
            default:
                kind = DidwwPricesErrorKind.UnexpectedStatus;
                break;
        }
        return new DidwwPricesException(kind, status, code, errors, retryAfter);
    }

    // Reads at most MaxErrorBodySize bytes. A broken or timed-out body reads as
    // nothing, so the status and Retry-After still decide the error. The
    // caller's cancellation propagates with the caller's token.
    private static async Task<string> ReadErrorBodyAsync(HttpContent content, CancellationToken cancellationToken,
        CancellationToken readToken)
    {
        try
        {
            var bytes = await DidwwHttp.ReadLimitedAsync(content, MaxErrorBodySize, readToken)
                .ConfigureAwait(false);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }
        catch (System.Exception e) when (IsTransportError(e, cancellationToken))
        {
            return "";
        }
    }

    // The error objects of a JSON:API errors document; none for any other body.
    private static List<ApiError> ParseErrors(string body)
    {
        var errors = new List<ApiError>();
        JToken root;
        try
        {
            using var reader = new JsonTextReader(new StringReader(body)) { DateParseHandling = DateParseHandling.None };
            root = JToken.Load(reader);
        }
        catch (JsonException)
        {
            return errors;
        }
        if (root is not JObject rootObject || rootObject["errors"] is not JArray items)
            return errors;

        foreach (var item in items.OfType<JObject>())
        {
            try
            {
                if (item.ToObject<ApiError>() is { } error)
                    errors.Add(error);
            }
            catch (System.Exception e) when (e is JsonException or FormatException or ArgumentException or InvalidCastException)
            {
                // Not an error object: skipped.
            }
        }
        return errors;
    }
}

// Non-generic so that every PricesService<TPrice> shares one instance.
internal static class PricesServiceConstants
{
    internal static readonly TimeSpan MaxUnavailableRetryAfter = TimeSpan.FromSeconds(60);
}
