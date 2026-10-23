using System.Text;
using Didww.Api3.Auth;
using Didww.Api3.Exception;
using FluentAssertions;

namespace Didww.Api3.Tests;

/// <summary>
/// Price requests over real sockets: the token service is WireMock, the prices
/// host a raw HTTP server that sends exactly the bytes given.
/// </summary>
public class PricesWireTest : BaseTest
{
    private static readonly byte[] Fixture = PricesServer.VoiceInFixture();

    // A stalled body must end on the client timeout; the guard fails a hang
    // instead of blocking the run.
    private static readonly TimeSpan StallGuard = TimeSpan.FromSeconds(15);

    private static readonly Dictionary<string, string?> LookupFilters = new() { ["number"] = "18005550123" };

    private static string FileHeaders(int status, string reason, int? contentLength) =>
        $"HTTP/1.1 {status} {reason}\r\n" +
        "Content-Type: application/gzip\r\n" +
        "Content-Disposition: attachment; filename=\"voice_in_prices.csv.gz\"\r\n" +
        $"Last-Modified: {PricesServer.LastModifiedHeader}\r\n" +
        (contentLength.HasValue ? $"Content-Length: {contentLength}\r\n" : "") +
        "Connection: close\r\n\r\n";

    private static string PriceFile(RawRequest request) =>
        FileHeaders(200, "OK", Fixture.Length) + (request.Method == "HEAD" ? "" : Encoding.Latin1.GetString(Fixture));

    private DidwwClient NewClient(RawHttpServer prices, HttpMessageHandler? innerHandler = null)
    {
        var builder = NewClientBuilder().SetVoiceInPricesUrl(prices.Url);
        if (innerHandler != null)
            builder.SetInnerHandler(innerHandler);
        return builder.Build();
    }

    [Fact]
    public async Task TestPriceFileDownloadOverHttp()
    {
        using var prices = new RawHttpServer((_, request) => PriceFile(request));
        var client = NewClient(prices);
        using var destination = new MemoryStream();

        var file = await client.VoiceInPrices().DownloadAsync(destination, PricesServer.LastModified.AddHours(2));

        destination.ToArray().Should().Equal(Fixture, "gzip is not decoded");
        file.StatusCode.Should().Be(200);
        file.ContentLength.Should().Be(Fixture.Length);
        file.LastModified.Should().Be(PricesServer.LastModified);
        file.FileName.Should().Be("voice_in_prices.csv.gz");
        var request = prices.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be("GET");
        request.Path.Should().Be("/v3/prices.csv.gz");
        request.Headers["Authorization"].Should().Be("Bearer " + TestAccessToken);
        request.Headers["If-Modified-Since"].Should().Be("Wed, 14 Oct 2026 08:00:00 GMT");
        request.Headers.Should().NotContainKey("If-None-Match");
        request.Headers.Should().NotContainKey("Accept");
        request.Headers.Should().NotContainKey("Api-Key");
    }

    [Fact]
    public async Task TestPriceFileHeadOverHttp()
    {
        using var prices = new RawHttpServer((_, request) => PriceFile(request));
        var client = NewClient(prices);

        var file = await client.VoiceInPrices().HeadAsync();

        prices.Requests.Should().ContainSingle().Which.Method.Should().Be("HEAD");
        file.StatusCode.Should().Be(200);
        file.ContentLength.Should().Be(Fixture.Length, "HEAD carries the Content-Length of the file");
        file.LastModified.Should().Be(PricesServer.LastModified);
        file.BytesWritten.Should().Be(0);
    }

    [Fact]
    public async Task TestPriceFileNotModifiedOverHttp()
    {
        using var prices = new RawHttpServer((_, _) => FileHeaders(304, "Not Modified", null));
        var client = NewClient(prices);
        using var destination = new MemoryStream();

        var file = await client.VoiceInPrices().DownloadAsync(destination, PricesServer.LastModified);

        file.NotModified.Should().BeTrue();
        file.ContentLength.Should().BeNull("a 304 has no length");
        file.LastModified.Should().Be(PricesServer.LastModified);
        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task TestPriceFileDownloadTruncatedBody()
    {
        using var prices = new RawHttpServer((_, _) => FileHeaders(200, "OK", 1000) + "0123456789");
        var client = NewClient(prices);
        using var dir = new TempDirectory();
        var path = dir.File("voice_in_prices.csv.gz");
        File.WriteAllText(path, "old copy");

        var act = () => client.VoiceInPrices().DownloadToFileAsync(path);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().Be(200);
        File.ReadAllText(path).Should().Be("old copy");
        dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }

    [Fact]
    public async Task TestLookupTruncatedErrorBodyKeepsStatus()
    {
        using var prices = new RawHttpServer((_, _) =>
            "HTTP/1.1 429 Too Many Requests\r\nContent-Type: application/vnd.api+json\r\nRetry-After: 7\r\n" +
            "Content-Length: 200\r\nConnection: close\r\n\r\n{\"errors\":[");
        var client = NewClient(prices);

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.TooManyRequests);
        e.StatusCode.Should().Be(429);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task TestLookupTruncatedBody()
    {
        using var prices = new RawHttpServer((_, _) =>
            "HTTP/1.1 200 OK\r\nContent-Type: application/vnd.api+json\r\nContent-Length: 200\r\n" +
            "Connection: close\r\n\r\n{\"data\":[");
        var client = NewClient(prices);

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task TestLookupStalledBodyTimesOut()
    {
        using var prices = new RawHttpServer((_, _) =>
            "HTTP/1.1 200 OK\r\nContent-Type: application/vnd.api+json\r\nContent-Length: 200\r\n" +
            "Connection: close\r\n\r\n{\"data\":[", hold: true);
        var client = NewClientBuilder().SetVoiceInPricesUrl(prices.Url).SetTimeout(TimeSpan.FromSeconds(1)).Build();
        await client.Tokens.GetTokenAsync(TokenAudience.VoiceIn, CancellationToken.None); // the 1 s covers the price host only

        var act = () => client.VoiceInPrices().LookupAsync(LookupFilters).WaitAsync(StallGuard);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task TestLookupStalledErrorBodyKeepsStatus()
    {
        using var prices = new RawHttpServer((_, _) =>
            "HTTP/1.1 429 Too Many Requests\r\nContent-Type: application/vnd.api+json\r\nRetry-After: 7\r\n" +
            "Content-Length: 200\r\nConnection: close\r\n\r\n{\"errors\":[", hold: true);
        var client = NewClientBuilder().SetVoiceInPricesUrl(prices.Url).SetTimeout(TimeSpan.FromSeconds(1)).Build();
        await client.Tokens.GetTokenAsync(TokenAudience.VoiceIn, CancellationToken.None); // the 1 s covers the price host only

        var act = () => client.VoiceInPrices().LookupAsync(LookupFilters).WaitAsync(StallGuard);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.TooManyRequests);
        e.StatusCode.Should().Be(429);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task TestLookupCancelledDuringBodyCarriesCallerToken()
    {
        var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var prices = new RawHttpServer((_, _) =>
        {
            answered.TrySetResult();
            return "HTTP/1.1 200 OK\r\nContent-Type: application/vnd.api+json\r\nContent-Length: 200\r\n" +
                "Connection: close\r\n\r\n{\"data\":[";
        }, hold: true);
        var client = NewClient(prices);
        using var cts = new CancellationTokenSource();

        var lookup = client.VoiceInPrices().LookupAsync(LookupFilters, cts.Token);
        await answered.Task.WaitAsync(StallGuard);
        await Task.Delay(TimeSpan.FromMilliseconds(200));
        cts.Cancel();
        var act = () => lookup.WaitAsync(StallGuard);

        var e = (await act.Should().ThrowAsync<OperationCanceledException>()).Which;
        e.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task TestPriceFileStalledErrorBodyKeepsStatus()
    {
        using var prices = new RawHttpServer((_, _) =>
            "HTTP/1.1 404 Not Found\r\nContent-Type: application/vnd.api+json\r\n" +
            "Content-Length: 200\r\nConnection: close\r\n\r\n{\"errors\":[", hold: true);
        var client = NewClientBuilder().SetVoiceInPricesUrl(prices.Url).SetTimeout(TimeSpan.FromSeconds(1)).Build();
        await client.Tokens.GetTokenAsync(TokenAudience.VoiceIn, CancellationToken.None); // the 1 s covers the price host only

        var act = () => client.VoiceInPrices().DownloadAsync(Stream.Null).WaitAsync(StallGuard);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.StatusCode.Should().Be(404);
        prices.Requests.Should().ContainSingle("a 404 is not retried");
    }

    [Fact]
    public async Task TestPriceFileDoesNotFollowRedirects()
    {
        using var prices = new RawHttpServer((_, request) => request.Path == "/v3/prices.csv.gz"
            ? "HTTP/1.1 302 Found\r\nLocation: /v3/elsewhere.csv.gz\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
            : PriceFile(request));
        var client = NewClient(prices);
        using var destination = new MemoryStream();

        var act = () => client.VoiceInPrices().DownloadAsync(destination);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.UnexpectedStatus);
        e.StatusCode.Should().Be(302);
        prices.Requests.Should().ContainSingle();
        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task TestPricesRefuseRedirectFollowedByInnerHandler()
    {
        using var prices = new RawHttpServer((_, request) => request.Path.StartsWith("/v3/elsewhere", StringComparison.Ordinal)
            ? request.Path.Contains("lookup", StringComparison.Ordinal)
                ? "HTTP/1.1 200 OK\r\nContent-Type: application/vnd.api+json\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"
                : PriceFile(request)
            : request.Path.StartsWith("/v3/lookup", StringComparison.Ordinal)
                ? "HTTP/1.1 302 Found\r\nLocation: /v3/elsewhere/lookup\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                : "HTTP/1.1 302 Found\r\nLocation: /v3/elsewhere.csv.gz\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        using var followingHandler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = NewClient(prices, followingHandler);
        using var destination = new MemoryStream();

        var download = () => client.VoiceInPrices().DownloadAsync(destination);
        var lookup = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await download.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.UnexpectedStatus);
        e.Message.Should().Contain("redirected");
        destination.Length.Should().Be(0, "nothing from the redirect target is written");
        (await lookup.Should().ThrowAsync<DidwwPricesException>()).Which.Kind
            .Should().Be(DidwwPricesErrorKind.UnexpectedStatus);
    }

    [Fact]
    public async Task TestPriceFileConnectionRefused()
    {
        var client = NewClientBuilder().SetVoiceInPricesUrl(RawHttpServer.ClosedUrl()).Build();

        var act = () => client.VoiceInPrices().DownloadAsync(Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().BeNull();
        e.InnerException.Should().BeOfType<HttpRequestException>();
    }
}
