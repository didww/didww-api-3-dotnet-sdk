using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Auth;

/// <summary>
/// Mints access tokens per audience, caches them in the token store and lets
/// concurrent callers share one mint.
/// </summary>
internal sealed class TokenProvider
{
    internal const string CacheKeyPrefix = "didww:token:v1:";
    internal const int MaxMintAttempts = 4;
    internal const int MaxErrorBodySize = 1 << 20;
    internal static readonly TimeSpan DefaultMintTimeout = TimeSpan.FromSeconds(10);

    // A store call abandoned on a timeout may fail later; observe it so that the
    // failure does not surface as an unobserved task exception.
    private static T Observed<T>(T task) where T : Task
    {
        _ = task.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return task;
    }
    internal static readonly TimeSpan DefaultStoreTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BackoffBase = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan BackoffCap = TimeSpan.FromSeconds(8);
    private const string StoreReadFailedMessage = "DIDWW token store read failed";

    // The 429 windows of the token service per auth URL and API key, shared by
    // every client of the process. Values are TimeProvider timestamps.
    private static readonly object RateLimitsLock = new();
    private static readonly Dictionary<string, long> RateLimits = new();

    private readonly string _authUrl;
    private readonly string _apiKey;
    private readonly string _apiKeyDigest;
    private readonly string _apiVersion;
    private readonly string? _scope;
    private readonly ITokenStore _store;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    private readonly object _mu = new();
    private readonly Dictionary<string, Task<DidwwToken>> _inflight = new();
    // Per cache key, the store lock keeps the write of a minted token and the
    // compare-and-delete of InvalidateAsync atomic against each other within
    // this client; clients or processes sharing a store are not serialized.
    private readonly Dictionary<string, SemaphoreSlim> _storeLocks = new();

    internal TokenProvider(string authUrl, string apiKey, string apiVersion, DidwwScope? scope, ITokenStore store,
        HttpMessageHandler innerHandler, ILogger logger)
    {
        _authUrl = authUrl.TrimEnd('/');
        _apiKey = apiKey;
        _apiKeyDigest = Sha256Hex(apiKey);
        _apiVersion = apiVersion;
        _scope = scope switch
        {
            DidwwScope.Read => "read",
            DidwwScope.Full => "full",
            _ => null
        };
        _store = store;
        _logger = logger;
        // No bearer handler and no total timeout: every attempt is bounded by MintTimeout.
        _httpClient = new HttpClient(innerHandler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
    }

    // Test knobs.
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;
    internal Func<double> NextDouble { get; set; } = Random.Shared.NextDouble;
    internal Func<TimeSpan, CancellationToken, Task>? Delay { get; set; }
    internal TimeSpan MintTimeout { get; set; } = DefaultMintTimeout;
    internal TimeSpan StoreTimeout { get; set; } = DefaultStoreTimeout;

    private Func<TimeSpan, CancellationToken, Task> DefaultDelay => (delay, ct) => Task.Delay(delay, TimeProvider, ct);

    /// <summary>
    /// Returns the cached token for <paramref name="audience"/> or mints one.
    /// Concurrent callers for the same audience wait for a single mint, each
    /// within its own cancellation token; cancelling a waiter does not cancel the mint.
    /// </summary>
    internal async Task<DidwwToken> GetTokenAsync(string audience, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CacheKey(audience);
        var cached = await ReadCachedAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached != null)
            return cached;
        cancellationToken.ThrowIfCancellationRequested();

        Task<DidwwToken> flight;
        lock (_mu)
        {
            if (!_inflight.TryGetValue(key, out flight!))
            {
                flight = Task.Run(() => FlyAsync(key, audience), CancellationToken.None);
                _ = flight.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                _inflight[key] = flight;
            }
        }
        return await flight.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Evicts the cached token for <paramref name="audience"/> only while it is
    /// still the refused one, so a token another caller has just minted survives.
    /// It waits for the store lock within <paramref name="cancellationToken"/> and
    /// bounds the store calls by <see cref="StoreTimeout"/>.
    /// </summary>
    internal async Task InvalidateAsync(string audience, string refusedAccessToken, CancellationToken cancellationToken)
    {
        var key = CacheKey(audience);
        var storeLock = StoreLock(key);
        try
        {
            await storeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(StoreTimeout);
            DidwwToken? token;
            try
            {
                var read = Observed(_store.GetAsync(key, cts.Token).AsTask());
                token = await read.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (System.Exception e)
            {
                _logger.LogWarning(e, StoreReadFailedMessage);
                return;
            }

            if (token == null || token.AccessToken != refusedAccessToken)
                return;

            try
            {
                var delete = Observed(_store.DeleteAsync(key, cts.Token).AsTask());
                await delete.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (System.Exception e)
            {
                _logger.LogWarning(e, "DIDWW token store delete failed");
            }
        }
        finally
        {
            storeLock.Release();
        }
    }

    internal string CacheKey(string audience)
    {
        var material = string.Join("\n", _authUrl, _apiKeyDigest, audience, _apiVersion, _scope ?? "-");
        return CacheKeyPrefix + Sha256Hex(material);
    }

    internal string RateLimitKey => _authUrl + "\n" + _apiKeyDigest;

    /// <summary>Removes the 429 window of this auth URL and API key (tests).</summary>
    internal void ForgetRateLimit()
    {
        lock (RateLimitsLock)
        {
            RateLimits.Remove(RateLimitKey);
        }
    }

    /// <summary>Opens a 429 window ending after <paramref name="duration"/> (tests).</summary>
    internal void BlockFor(TimeSpan duration)
    {
        lock (RateLimitsLock)
        {
            RateLimits[RateLimitKey] = TimeProvider.GetTimestamp() + ToTimestampTicks(duration);
        }
    }

    internal TimeSpan Backoff(int attempt)
    {
        var limit = TimeSpan.FromTicks(Math.Min(BackoffCap.Ticks, BackoffBase.Ticks << Math.Min(attempt, 16)));
        return TimeSpan.FromTicks((long)(NextDouble() * limit.Ticks));
    }

    /// <summary>
    /// Returns <paramref name="retryAfter"/> plus a random extra of up to a fifth
    /// of it, or up to a second when that is more, so that clients told the same
    /// Retry-After do not come back in step.
    /// </summary>
    internal static TimeSpan SpreadRetryAfter(TimeSpan retryAfter, Func<double> nextDouble)
    {
        var maxExtra = Math.Max(TimeSpan.FromSeconds(1).Ticks, retryAfter.Ticks / 5);
        return retryAfter + TimeSpan.FromTicks((long)(nextDouble() * maxExtra));
    }

    private async Task<DidwwToken?> ReadCachedAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var read = Observed(_store.GetAsync(key, cancellationToken).AsTask());
            var token = await read.WaitAsync(cancellationToken).ConfigureAwait(false);
            return string.IsNullOrEmpty(token?.AccessToken) ? null : token;
        }
        catch (System.Exception e)
        {
            if (!cancellationToken.IsCancellationRequested)
                _logger.LogWarning(e, StoreReadFailedMessage);
            return null;
        }
    }

    // Runs detached from every caller: a waiter that gives up does not cancel it.
    // An exception thrown here (from the store, the logger or the handler)
    // becomes the result of the flight for every waiter.
    private async Task<DidwwToken> FlyAsync(string key, string audience)
    {
        try
        {
            var cached = await ReadCachedWithinAsync(key).ConfigureAwait(false);
            if (cached != null)
                return cached;

            var token = await MintAsync(audience).ConfigureAwait(false);
            await StoreTokenAsync(key, token).ConfigureAwait(false);
            return token;
        }
        finally
        {
            lock (_mu)
            {
                _inflight.Remove(key);
            }
        }
    }

    private async Task<DidwwToken?> ReadCachedWithinAsync(string key)
    {
        using var cts = new CancellationTokenSource(StoreTimeout);
        try
        {
            var read = Observed(_store.GetAsync(key, cts.Token).AsTask());
            var token = await read.WaitAsync(cts.Token).ConfigureAwait(false);
            return string.IsNullOrEmpty(token?.AccessToken) ? null : token;
        }
        catch (System.Exception e)
        {
            _logger.LogWarning(e, StoreReadFailedMessage);
            return null;
        }
    }

    private async Task StoreTokenAsync(string key, DidwwToken token)
    {
        using var cts = new CancellationTokenSource(StoreTimeout);
        var storeLock = StoreLock(key);
        try
        {
            await storeLock.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(e, "DIDWW token store write skipped");
            return;
        }

        try
        {
            var write = Observed(_store.SetAsync(key, token, TimeSpan.FromSeconds(token.ExpiresIn), cts.Token).AsTask());
            await write.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (System.Exception e)
        {
            _logger.LogWarning(e, "DIDWW token store write failed");
        }
        finally
        {
            storeLock.Release();
        }
    }

    internal SemaphoreSlim StoreLock(string key)
    {
        lock (_mu)
        {
            if (!_storeLocks.TryGetValue(key, out var storeLock))
            {
                storeLock = new SemaphoreSlim(1, 1);
                _storeLocks[key] = storeLock;
            }
            return storeLock;
        }
    }

    private async Task<DidwwToken> MintAsync(string audience)
    {
        ThrowIfRateLimited();

        var body = JsonConvert.SerializeObject(new TokenRequest(new[] { audience }, _apiVersion, _scope),
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        var start = TimeProvider.GetTimestamp();
        var response = await PostAsync(body).ConfigureAwait(false);
        var token = TokenFrom(response, audience);
        _logger.LogDebug(
            "DIDWW token minted: aud {Audience}, scope {Scope}, api_version {ApiVersion}, expires_in {ExpiresIn}, duration {Duration}",
            string.Join(" ", token.Audience), token.Scope, token.ApiVersion, token.ExpiresIn,
            TimeProvider.GetElapsedTime(start));
        return token;
    }

    // Sends the token request, retrying 5xx answers and transport failures
    // other than TLS errors with a full-jitter backoff.
    private async Task<TokenResponse> PostAsync(string body)
    {
        if (!Uri.TryCreate(_authUrl + "/token", UriKind.Absolute, out var tokenUri))
            throw new DidwwAuthException(DidwwAuthErrorKind.ConnectionError, errorDescription: "invalid token service URL");

        var attempt = 0;
        while (true)
        {
            attempt++;
            TokenResponse? response = null;
            System.Exception? failure = null;
            try
            {
                response = await AttemptAsync(tokenUri, body).ConfigureAwait(false);
            }
            catch (System.Exception e) when (e is HttpRequestException or OperationCanceledException or IOException)
            {
                failure = e;
            }

            if (response != null && (response.Status < 500 || attempt == MaxMintAttempts))
                return response;
            if (failure != null && (IsTlsError(failure) || attempt == MaxMintAttempts))
                throw new DidwwAuthException(DidwwAuthErrorKind.ConnectionError, innerException: failure);

            await (Delay ?? DefaultDelay)(Backoff(attempt), CancellationToken.None).ConfigureAwait(false);
            ThrowIfRateLimited();
        }
    }

    private async Task<TokenResponse> AttemptAsync(Uri tokenUri, string body)
    {
        using var cts = new CancellationTokenSource(MintTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("Api-Key", _apiKey);
        request.Headers.TryAddWithoutValidation("User-Agent", DidwwClient.SdkUserAgent);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
            .ConfigureAwait(false);
        if (DidwwHttp.WasRedirected(response, tokenUri))
        {
            throw new DidwwAuthException(DidwwAuthErrorKind.UnexpectedStatus, (int)response.StatusCode,
                errorDescription: "the token request was redirected; redirects are not followed");
        }
        var bytes = await DidwwHttp.ReadLimitedAsync(response.Content, MaxErrorBodySize, cts.Token).ConfigureAwait(false);
        return new TokenResponse((int)response.StatusCode, response.Headers.RetryAfter?.Delta, bytes);
    }

    private DidwwToken TokenFrom(TokenResponse response, string audience)
    {
        var text = Encoding.UTF8.GetString(response.Body);
        JObject? fields = null;
        try
        {
            fields = JToken.Parse(text) as JObject;
        }
        catch (JsonException)
        {
            // Not JSON: no error fields.
        }
        var error = StringField(fields, "error");
        var description = StringField(fields, "error_description");

        switch (response.Status)
        {
            case (int)HttpStatusCode.OK:
                DidwwToken? token = null;
                try
                {
                    token = fields == null ? null : fields.ToObject<DidwwToken>();
                }
                catch (JsonException)
                {
                    // An unusable body is reported by VerifyGrant.
                }
                catch (ArgumentException)
                {
                    // Likewise.
                }
                token ??= new DidwwToken();
                token.Audience ??= Array.Empty<string>();
                token.AccessToken ??= "";
                token.MintedAt = TimeProvider.GetUtcNow();
                VerifyGrant(token, audience);
                return token;
            case (int)HttpStatusCode.BadRequest:
                throw new DidwwAuthException(DidwwAuthErrorKind.InvalidRequest, response.Status, error, description);
            case (int)HttpStatusCode.Unauthorized:
                throw new DidwwAuthException(DidwwAuthErrorKind.InvalidClient, response.Status, error, description);
            case (int)HttpStatusCode.TooManyRequests:
                var retryAfter = TokenRetryAfter(response.RetryAfter);
                lock (RateLimitsLock)
                {
                    RateLimits[RateLimitKey] = TimeProvider.GetTimestamp() +
                                               ToTimestampTicks(SpreadRetryAfter(retryAfter, NextDouble));
                }
                throw new DidwwAuthException(DidwwAuthErrorKind.RateLimited, response.Status, error, description,
                    retryAfter);
            case >= 500 and <= 599:
                throw new DidwwAuthException(DidwwAuthErrorKind.ServerError, response.Status, error, description);
            default:
                throw new DidwwAuthException(DidwwAuthErrorKind.UnexpectedStatus, response.Status, error, description);
        }
    }

    private void VerifyGrant(DidwwToken token, string audience)
    {
        if (string.IsNullOrEmpty(token.AccessToken) || !token.Audience.Contains(audience))
        {
            throw new DidwwAuthException(DidwwAuthErrorKind.InvalidGrant, (int)HttpStatusCode.OK,
                errorDescription: $"granted audience [{string.Join(" ", token.Audience)}], requested {audience}");
        }
        if (_scope != null && token.Scope != _scope)
        {
            _logger.LogWarning("DIDWW token scope mismatch: requested {Requested}, granted {Granted}",
                _scope, token.Scope);
        }
        if (token.ApiVersion != _apiVersion)
        {
            _logger.LogWarning("DIDWW token api_version mismatch: requested {Requested}, granted {Granted}",
                _apiVersion, token.ApiVersion);
        }
    }

    // Throws RateLimited while a 429 window of the token service is open, so
    // that no request is sent inside it.
    private void ThrowIfRateLimited()
    {
        long until;
        lock (RateLimitsLock)
        {
            if (!RateLimits.TryGetValue(RateLimitKey, out until))
                return;
        }
        var now = TimeProvider.GetTimestamp();
        if (now >= until)
            return;
        var remaining = TimeProvider.GetElapsedTime(now, until);
        throw new DidwwAuthException(DidwwAuthErrorKind.RateLimited, (int)HttpStatusCode.TooManyRequests,
            "rate_limit_exceeded", retryAfter: TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds)));
    }

    private long ToTimestampTicks(TimeSpan duration) =>
        (long)(duration.TotalSeconds * TimeProvider.TimestampFrequency);

    // The Retry-After seconds of a 429, at least one second; an HTTP date or a
    // missing value counts as one second.
    private static TimeSpan TokenRetryAfter(TimeSpan? delta)
    {
        if (delta is not { } value || value < TimeSpan.FromSeconds(1))
            return TimeSpan.FromSeconds(1);
        return TimeSpan.FromSeconds(Math.Floor(value.TotalSeconds));
    }

    /// <summary>
    /// Whether <paramref name="exception"/> is a certificate or TLS protocol
    /// failure, which retrying cannot fix.
    /// </summary>
    internal static bool IsTlsError(System.Exception exception)
    {
        for (var e = exception; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError })
                return true;
            if (e is AuthenticationException)
                return true;
        }
        return false;
    }

    private static string? StringField(JObject? fields, string name) =>
        fields?[name] is JValue { Type: JTokenType.String } value ? (string?)value : null;

    private static string Sha256Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record TokenResponse(int Status, TimeSpan? RetryAfter, byte[] Body);

    private sealed class TokenRequest
    {
        public TokenRequest(string[] audience, string apiVersion, string? scope)
        {
            Audience = audience;
            ApiVersion = apiVersion;
            Scope = scope;
        }

        [JsonProperty("aud")]
        public string[] Audience { get; }

        [JsonProperty("api_version")]
        public string ApiVersion { get; }

        [JsonProperty("scope")]
        public string? Scope { get; }
    }
}
