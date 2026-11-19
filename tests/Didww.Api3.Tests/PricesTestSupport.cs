using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Didww.Api3.Prices;
using Didww.Api3.Resource;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

/// <summary>
/// A <see cref="FakeAuthServer"/> whose clients have one host per prices
/// service and which answers the n-th (1-based) request other than a token
/// request with <see cref="Respond"/>.
/// </summary>
internal sealed class PricesServer : IDisposable
{
    public const string VoiceInUrl = "https://vin.example.test";
    public const string VoiceOutUrl = "https://vout.example.test";
    public const string SmsUrl = "https://sms.example.test";

    public const string LastModifiedHeader = "Wed, 14 Oct 2026 06:00:00 GMT";
    public const string OtherLastModifiedHeader = "Thu, 15 Oct 2026 06:00:00 GMT";
    public static readonly DateTimeOffset LastModified = new(2026, 10, 14, 6, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset OtherLastModified = new(2026, 10, 15, 6, 0, 0, TimeSpan.Zero);

    public PricesServer(Func<int, RecordedRequest, HttpResponseMessage> respond)
    {
        Respond = respond;
        Auth.Api = (request, _) =>
        {
            var n = Auth.ApiRequests.Count;
            return Task.FromResult(Respond(n, request));
        };
    }

    public FakeAuthServer Auth { get; } = new();

    public Func<int, RecordedRequest, HttpResponseMessage> Respond { get; set; }

    public List<RecordedRequest> Requests => Auth.ApiRequests;

    public DidwwClient NewClient(Action<DidwwClient.Builder>? configure = null) => Auth.NewClient(builder =>
    {
        builder.SetVoiceInPricesUrl(VoiceInUrl).SetVoiceOutPricesUrl(VoiceOutUrl).SetSmsPricesUrl(SmsUrl);
        configure?.Invoke(builder);
    });

    /// <summary>The audiences of every token request, in order.</summary>
    public List<string> MintAudiences()
    {
        var audiences = new List<string>();
        for (var i = 0; i < Auth.Mints; i++)
            audiences.AddRange(JObject.Parse(Auth.MintBody(i))["aud"]!.Values<string>().Select(a => a!));
        return audiences;
    }

    public void Dispose() => Auth.Dispose();

    public static byte[] VoiceInFixture() => LoadBytes("prices/voice_in_prices.csv.gz");

    public static byte[] LoadBytes(string path) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixtures", path));

    /// <summary>A price file answer; a HEAD answer has the headers only.</summary>
    public static HttpResponseMessage PriceFile(RecordedRequest request, byte[] body,
        string lastModified = LastModifiedHeader)
    {
        var content = new ByteArrayContent(request.Method == "HEAD" ? Array.Empty<byte>() : body);
        content.Headers.TryAddWithoutValidation("Content-Type", "application/gzip");
        content.Headers.TryAddWithoutValidation("Content-Disposition", "attachment; filename=\"voice_in_prices.csv.gz\"");
        content.Headers.TryAddWithoutValidation("Content-Length", body.Length.ToString());
        content.Headers.TryAddWithoutValidation("Last-Modified", lastModified);
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        response.Headers.TryAddWithoutValidation("Cache-Control", "private");
        response.Headers.TryAddWithoutValidation("Accept-Ranges", "none");
        return response;
    }

    /// <summary>A 304 answer with the headers of a price file.</summary>
    public static HttpResponseMessage NotModified(string lastModified = LastModifiedHeader)
    {
        var content = new ByteArrayContent(Array.Empty<byte>());
        content.Headers.TryAddWithoutValidation("Content-Disposition", "attachment; filename=\"voice_in_prices.csv.gz\"");
        content.Headers.TryAddWithoutValidation("Last-Modified", lastModified);
        return new HttpResponseMessage(HttpStatusCode.NotModified) { Content = content };
    }

    /// <summary>A JSON:API answer, with a Retry-After when given.</summary>
    public static HttpResponseMessage JsonApi(int status, string body, string? retryAfter = null)
    {
        var response = FakeAuthServer.Json(status, body, "application/vnd.api+json");
        if (retryAfter != null)
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        return response;
    }

    /// <summary>A 503 answer with the serving failure fixture.</summary>
    public static HttpResponseMessage ServingFailure(string? retryAfter) =>
        JsonApi(503, FakeAuthServer.LoadFixture("prices/errors/serving_failure.json"), retryAfter);
}

/// <summary>Recorded 503 waits of a prices service.</summary>
internal sealed class RecordedWaits
{
    private readonly ConcurrentQueue<TimeSpan> _waits = new();

    public List<TimeSpan> All => _waits.ToList();

    /// <summary>Replaces the 503 wait and jitter of <paramref name="service"/>, returning the recorded waits.</summary>
    public static RecordedWaits Stub<T>(PricesService<T> service, double jitter) where T : BaseResource
    {
        var recorded = new RecordedWaits();
        service.NextDouble = () => jitter;
        service.Delay = (wait, _) =>
        {
            recorded._waits.Enqueue(wait);
            return Task.CompletedTask;
        };
        return recorded;
    }
}

/// <summary>A stream whose reads and writes throw a given exception.</summary>
internal sealed class ThrowingStream : Stream
{
    private readonly System.Exception _error;

    public ThrowingStream(System.Exception error)
    {
        _error = error;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw _error;
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw _error;
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        throw _error;
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw _error;
}

/// <summary>A temporary directory deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "didww-prices-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <summary>The names in the directory, sorted.</summary>
    public List<string> Entries() => Directory.GetFileSystemEntries(Path)
        .Select(System.IO.Path.GetFileName)
        .Select(name => name!)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToList();

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>
/// A minimal HTTP/1.1 server on a loopback socket: each connection carries one
/// request (GET or HEAD, no body), answered with the raw bytes of
/// <see cref="Respond"/> and closed, so that answers can be truncated or carry
/// exactly the headers given. With <c>hold</c> the connection stays open after
/// the answer until the server is disposed, so that a body can stall.
/// </summary>
internal sealed class RawHttpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly ConcurrentQueue<RawRequest> _requests = new();
    private readonly bool _hold;
    private int _count;

    public RawHttpServer(Func<int, RawRequest, string> respond, bool hold = false)
    {
        Respond = respond;
        _hold = hold;
        _listener.Start();
        _ = Task.Run(AcceptLoopAsync);
    }

    public Func<int, RawRequest, string> Respond { get; set; }

    public string Url => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    public List<RawRequest> Requests => _requests.ToList();

    private async Task AcceptLoopAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (System.Exception)
            {
                return;
            }
            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var head = await ReadHeadAsync(stream);
                if (head == null)
                    return;
                var lines = head.Split("\r\n");
                var requestLine = lines[0].Split(' ');
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in lines.Skip(1).Where(l => l.Length > 0))
                {
                    var colon = line.IndexOf(':');
                    headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
                }
                var request = new RawRequest(requestLine[0], requestLine[1], headers);
                _requests.Enqueue(request);
                var answer = Respond(Interlocked.Increment(ref _count), request);
                var bytes = Encoding.Latin1.GetBytes(answer);
                await stream.WriteAsync(bytes, _stop.Token);
                await stream.FlushAsync(_stop.Token);
                if (_hold)
                    await Task.Delay(Timeout.Infinite, _stop.Token);
            }
            catch (System.Exception)
            {
                // The client went away, or the server stopped.
            }
        }
    }

    private static async Task<string?> ReadHeadAsync(NetworkStream stream)
    {
        var buffer = new List<byte>();
        var chunk = new byte[1];
        while (true)
        {
            var read = await stream.ReadAsync(chunk);
            if (read == 0)
                return null;
            buffer.Add(chunk[0]);
            var n = buffer.Count;
            if (n >= 4 && buffer[n - 4] == '\r' && buffer[n - 3] == '\n' && buffer[n - 2] == '\r' && buffer[n - 1] == '\n')
                return Encoding.Latin1.GetString(buffer.ToArray(), 0, n - 4);
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }

    /// <summary>A URL on which nothing listens.</summary>
    public static string ClosedUrl()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return $"http://127.0.0.1:{port}";
    }
}

internal sealed record RawRequest(string Method, string Path, Dictionary<string, string> Headers);
