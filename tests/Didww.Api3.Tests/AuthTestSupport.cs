using System.Net;
using System.Text;
using Didww.Api3.Auth;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

/// <summary>
/// An in-memory inner handler that serves POST {AuthUrl}/token through
/// <see cref="Mint"/> and every other request through <see cref="Api"/>,
/// recording both. Clients built by <see cref="NewClient"/> get a unique API
/// key, so that the process-wide 429 window of one test never blocks another.
/// </summary>
internal sealed class FakeAuthServer : HttpMessageHandler
{
    public const string BaseUrl = "https://api.example.test/v3";
    public const string AuthUrl = "https://auth.example.test";

    private readonly object _lock = new();
    private readonly List<string> _mintBodies = new();
    private readonly List<Dictionary<string, string>> _mintHeaders = new();
    private readonly List<RecordedRequest> _apiRequests = new();
    private readonly List<DidwwClient> _clients = new();

    /// <summary>Answers the n-th (1-based) token request; by default grants "token-n".</summary>
    public Func<int, string, CancellationToken, Task<HttpResponseMessage>> Mint { get; set; } =
        (n, body, _) => Task.FromResult(TokenResponse(body, $"token-{n}"));

    /// <summary>Answers an API request; by default 404.</summary>
    public Func<RecordedRequest, CancellationToken, Task<HttpResponseMessage>>? Api { get; set; }

    public int Mints
    {
        get
        {
            lock (_lock)
            {
                return _mintBodies.Count;
            }
        }
    }

    public string MintBody(int index)
    {
        lock (_lock)
        {
            return _mintBodies[index];
        }
    }

    public Dictionary<string, string> MintHeaders(int index)
    {
        lock (_lock)
        {
            return _mintHeaders[index];
        }
    }

    public List<RecordedRequest> ApiRequests
    {
        get
        {
            lock (_lock)
            {
                return _apiRequests.ToList();
            }
        }
    }

    public DidwwClient NewClient(Action<DidwwClient.Builder>? configure = null)
    {
        var builder = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("key-" + Guid.NewGuid().ToString("N"), DidwwEnvironment.Sandbox))
            .SetBaseUrl(BaseUrl)
            .SetAuthUrl(AuthUrl)
            .SetLogger(null)
            .SetInnerHandler(this);
        configure?.Invoke(builder);
        var client = builder.Build();
        lock (_lock)
        {
            _clients.Add(client);
        }
        return client;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = Snapshot(request);
        HttpResponseMessage response;
        if (request.Method == HttpMethod.Post && uri.AbsolutePath == "/token" &&
            uri.GetLeftPart(UriPartial.Authority) == AuthUrl)
        {
            int n;
            lock (_lock)
            {
                _mintBodies.Add(body);
                _mintHeaders.Add(headers);
                n = _mintBodies.Count;
            }
            response = await Mint(n, body, cancellationToken);
        }
        else
        {
            var recorded = new RecordedRequest(request.Method.Method, uri, headers, body);
            lock (_lock)
            {
                _apiRequests.Add(recorded);
            }
            response = Api != null
                ? await Api(recorded, cancellationToken)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
        response.RequestMessage = request;
        return response;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_lock)
            {
                foreach (var client in _clients)
                    client.Tokens.ForgetRateLimit();
            }
        }
        base.Dispose(disposing);
    }

    /// <summary>A token answer echoing the requested audience and API version, scope "full", expires_in 900.</summary>
    public static HttpResponseMessage TokenResponse(string requestBody, string accessToken) =>
        Json(200, TokenJson(requestBody, accessToken));

    public static string TokenJson(string? requestBody, string accessToken)
    {
        JObject request;
        try
        {
            request = JObject.Parse(string.IsNullOrEmpty(requestBody) ? "{}" : requestBody);
        }
        catch (Newtonsoft.Json.JsonException)
        {
            request = new JObject();
        }
        return new JObject
        {
            ["access_token"] = accessToken,
            ["token_type"] = "Bearer",
            ["expires_in"] = 900,
            ["aud"] = request["aud"] ?? new JArray(),
            ["scope"] = "full",
            ["api_version"] = request["api_version"] ?? ""
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    public static HttpResponseMessage Json(int status, string body, string contentType = "application/json") =>
        new((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8, contentType) };

    public static HttpResponseMessage Fixture(int status, string fixturePath,
        string contentType = "application/vnd.api+json") =>
        Json(status, LoadFixture(fixturePath), contentType);

    public static string LoadFixture(string path) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixtures", path));

    private static Dictionary<string, string> Snapshot(HttpRequestMessage request)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
            headers[header.Key] = string.Join(", ", header.Value);
        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
                headers[header.Key] = string.Join(", ", header.Value);
        }
        return headers;
    }
}

internal sealed record RecordedRequest(string Method, Uri Uri, Dictionary<string, string> Headers, string Body)
{
    public string? Authorization => Headers.TryGetValue("Authorization", out var value) ? value : null;
}

/// <summary>A manually advanced clock for token expiry and 429 windows.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _lock = new();
    private DateTimeOffset _now = new(2000, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _now;
        }
    }

    public override long GetTimestamp() => GetUtcNow().UtcTicks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public void Advance(TimeSpan duration)
    {
        lock (_lock)
        {
            _now += duration;
        }
    }
}

/// <summary>An ILogger that keeps the formatted messages at or above a level.</summary>
internal sealed class ListLogger : ILogger
{
    private readonly object _lock = new();
    private readonly List<(LogLevel Level, string Message)> _entries = new();
    private readonly LogLevel _minimum;

    public ListLogger(LogLevel minimum = LogLevel.Trace)
    {
        _minimum = minimum;
    }

    public List<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    public string Text => string.Join("\n", Entries.Select(e => $"{e.Level}: {e.Message}"));

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimum;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception,
        Func<TState, System.Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        lock (_lock)
        {
            _entries.Add((logLevel, formatter(state, exception)));
        }
    }
}

/// <summary>Recorded backoff delays of a token provider.</summary>
internal sealed class RecordedDelays
{
    private readonly object _lock = new();
    private readonly List<TimeSpan> _delays = new();

    public List<TimeSpan> All
    {
        get
        {
            lock (_lock)
            {
                return _delays.ToList();
            }
        }
    }

    /// <summary>Replaces the provider's delay and jitter, returning the recorded delays.</summary>
    public static RecordedDelays Stub(DidwwClient client, double jitter)
    {
        var recorded = new RecordedDelays();
        client.Tokens.NextDouble = () => jitter;
        client.Tokens.Delay = (delay, _) =>
        {
            lock (recorded._lock)
            {
                recorded._delays.Add(delay);
            }
            return Task.CompletedTask;
        };
        return recorded;
    }
}

/// <summary>An ITokenStore wrapping a MemoryTokenStore whose calls can be overridden.</summary>
internal class DelegatingTokenStore : ITokenStore
{
    public DelegatingTokenStore(ITokenStore? inner = null)
    {
        Inner = inner ?? new MemoryTokenStore();
    }

    public ITokenStore Inner { get; }

    public Func<string, CancellationToken, ValueTask<DidwwToken?>>? OnGet { get; set; }
    public Func<string, DidwwToken, TimeSpan, CancellationToken, ValueTask>? OnSet { get; set; }
    public Func<string, CancellationToken, ValueTask>? OnDelete { get; set; }

    public virtual ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken) =>
        OnGet != null ? OnGet(key, cancellationToken) : Inner.GetAsync(key, cancellationToken);

    public virtual ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken) =>
        OnSet != null ? OnSet(key, token, ttl, cancellationToken) : Inner.SetAsync(key, token, ttl, cancellationToken);

    public virtual ValueTask DeleteAsync(string key, CancellationToken cancellationToken) =>
        OnDelete != null ? OnDelete(key, cancellationToken) : Inner.DeleteAsync(key, cancellationToken);
}
