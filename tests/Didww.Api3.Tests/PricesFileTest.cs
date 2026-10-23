using System.Net;
using System.Runtime.Versioning;
using System.Text;
using Didww.Api3.Exception;
using Didww.Api3.Prices;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class PricesFileTest : IDisposable
{
    private const string UnauthorizedBody =
        """{"errors":[{"title":"Unauthorized","detail":"Authorization failed","code":"401","status":"401"}]}""";

    private static readonly byte[] Fixture = PricesServer.VoiceInFixture();
    private static readonly byte[] OldCopy = Encoding.ASCII.GetBytes("old copy");

    private readonly TempDirectory _dir = new();

    public void Dispose()
    {
        _dir.Dispose();
        GC.SuppressFinalize(this);
    }

    private static PricesServer ServeFile() => new((_, request) => PricesServer.PriceFile(request, Fixture));

    private static Task<PriceFile> Download(DidwwClient client, string service, Stream destination) => service switch
    {
        "VoiceIn" => client.VoiceInPrices().DownloadAsync(destination),
        "VoiceOut" => client.VoiceOutPrices().DownloadAsync(destination),
        "SmsOutA2P" => client.SmsOutA2PPrices().DownloadAsync(destination),
        "SmsOutP2P" => client.SmsOutP2PPrices().DownloadAsync(destination),
        "SmsOutOtp" => client.SmsOutOtpPrices().DownloadAsync(destination),
        "SmsIn" => client.SmsInPrices().DownloadAsync(destination),
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    private void WriteOldCopy(string path, DateTimeOffset? modified = null)
    {
        File.WriteAllBytes(path, OldCopy);
        if (modified.HasValue)
            File.SetLastWriteTimeUtc(path, modified.Value.UtcDateTime);
    }

    // The permission bits of a file created now, i.e. 0666 minus the umask.
    [UnsupportedOSPlatform("windows")]
    private UnixFileMode NewFileMode()
    {
        var path = _dir.File("mode-probe-" + Guid.NewGuid().ToString("N"));
        using (new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        {
        }
        var mode = File.GetUnixFileMode(path);
        File.Delete(path);
        return mode;
    }

    [Theory]
    [InlineData("VoiceIn", "https://vin.example.test/v3/prices.csv.gz", "api-vin.didww.com")]
    [InlineData("VoiceOut", "https://vout.example.test/v3/prices.csv.gz", "api-vout.didww.com")]
    [InlineData("SmsOutA2P", "https://sms.example.test/v3/sms_out/a2p/prices.csv.gz", "api-sms.didww.com")]
    [InlineData("SmsOutP2P", "https://sms.example.test/v3/sms_out/p2p/prices.csv.gz", "api-sms.didww.com")]
    [InlineData("SmsOutOtp", "https://sms.example.test/v3/sms_out/otp/prices.csv.gz", "api-sms.didww.com")]
    [InlineData("SmsIn", "https://sms.example.test/v3/sms_in/prices.csv.gz", "api-sms.didww.com")]
    public async Task TestPriceFileEndpoints(string service, string url, string audience)
    {
        using var server = ServeFile();
        var client = server.NewClient();
        using var destination = new MemoryStream();

        await Download(client, service, destination);

        var request = server.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be("GET");
        request.Uri.ToString().Should().Be(url);
        request.Authorization.Should().Be("Bearer token-1");
        server.MintAudiences().Should().Equal(audience);
        destination.ToArray().Should().Equal(Fixture);
    }

    [Fact]
    public async Task TestPriceFileDownload()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        using var destination = new MemoryStream();

        var file = await client.VoiceInPrices().DownloadAsync(destination);

        destination.ToArray().Should().Equal(Fixture, "the gzipped bytes are written unchanged");
        file.StatusCode.Should().Be(200);
        file.NotModified.Should().BeFalse();
        file.LastModified.Should().Be(PricesServer.LastModified);
        file.ContentLength.Should().Be(Fixture.Length);
        file.FileName.Should().Be("voice_in_prices.csv.gz");
        file.BytesWritten.Should().Be(Fixture.Length);

        var request = server.Requests.Single();
        request.Headers.Should().NotContainKey("If-Modified-Since");
        request.Headers.Should().NotContainKey("If-None-Match");
        request.Headers.Should().NotContainKey("Api-Key");
        request.Headers.Should().NotContainKey("Accept");
        request.Headers["User-Agent"].Should().Be(DidwwClient.SdkUserAgent);
    }

    [Fact]
    public async Task TestPriceFileDownloadSendsIfModifiedSince()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        var local = new DateTimeOffset(2026, 10, 14, 8, 0, 0, 500, TimeSpan.FromHours(2));

        await client.VoiceInPrices().DownloadAsync(Stream.Null, local);

        server.Requests.Single().Headers["If-Modified-Since"].Should().Be(PricesServer.LastModifiedHeader);
    }

    [Fact]
    public async Task TestPriceFileDownloadNotModified()
    {
        using var server = new PricesServer((_, _) => PricesServer.NotModified());
        var client = server.NewClient();
        using var destination = new MemoryStream();

        var file = await client.VoiceInPrices().DownloadAsync(destination, PricesServer.LastModified);

        file.NotModified.Should().BeTrue();
        file.StatusCode.Should().Be(304);
        file.LastModified.Should().Be(PricesServer.LastModified);
        file.ContentLength.Should().BeNull();
        file.BytesWritten.Should().Be(0);
        destination.Length.Should().Be(0);
        server.Requests.Single().Headers["If-Modified-Since"].Should().Be(PricesServer.LastModifiedHeader);
    }

    [Fact]
    public async Task TestPriceFileDownloadRequiresDestination()
    {
        using var server = ServeFile();
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().DownloadAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
        server.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileHead()
    {
        using var server = ServeFile();
        var client = server.NewClient();

        var file = await client.VoiceInPrices().HeadAsync();

        var request = server.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be("HEAD");
        request.Uri.AbsolutePath.Should().Be("/v3/prices.csv.gz");
        request.Headers.Should().NotContainKey("If-Modified-Since");
        file.StatusCode.Should().Be(200);
        file.LastModified.Should().Be(PricesServer.LastModified);
        file.ContentLength.Should().Be(Fixture.Length);
        file.FileName.Should().Be("voice_in_prices.csv.gz");
        file.BytesWritten.Should().Be(0);
    }

    [Fact]
    public async Task TestPriceFileHeadNotModified()
    {
        using var server = new PricesServer((_, _) => PricesServer.NotModified());
        var client = server.NewClient();

        var file = await client.VoiceInPrices().HeadAsync(PricesServer.LastModified);

        file.NotModified.Should().BeTrue();
        file.LastModified.Should().Be(PricesServer.LastModified);
        server.Requests.Single().Headers["If-Modified-Since"].Should().Be(PricesServer.LastModifiedHeader);
    }

    [Fact]
    public async Task TestPriceFileHeadNotFoundWithoutBody()
    {
        using var server = new PricesServer((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().HeadAsync();

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.PricePlanNotAssigned);
        e.StatusCode.Should().Be(404);
        e.Code.Should().Be("price_plan_not_assigned");
        e.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileHeadRetriesServiceUnavailable()
    {
        using var server = new PricesServer((n, request) => n == 1
            ? PricesServer.JsonApi(503, "", "2")
            : PricesServer.PriceFile(request, Fixture));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, 0);

        var file = await prices.HeadAsync();

        file.StatusCode.Should().Be(200);
        waits.All.Should().Equal(TimeSpan.FromSeconds(2));
        server.Requests.Select(r => r.Method).Should().Equal("HEAD", "HEAD");
    }

    [Fact]
    public async Task TestPriceFileDownloadRetriesServiceUnavailable()
    {
        using var server = new PricesServer((n, request) => n == 1
            ? PricesServer.ServingFailure("1")
            : PricesServer.PriceFile(request, Fixture));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, 0);
        using var destination = new MemoryStream();

        await prices.DownloadAsync(destination);

        destination.ToArray().Should().Equal(Fixture, "the 503 body is not written");
        waits.All.Should().Equal(TimeSpan.FromSeconds(1));
        server.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestPriceFileDownloadServiceUnavailableAfterTwoRetries()
    {
        using var server = new PricesServer((_, _) => PricesServer.ServingFailure("1"));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, 0);
        using var destination = new MemoryStream();

        var act = () => prices.DownloadAsync(destination);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ServiceUnavailable);
        e.StatusCode.Should().Be(503);
        e.Code.Should().Be("serving_failure");
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(1));
        e.Message.Should().Be("Prices service: The price file is not available yet.");
        server.Requests.Should().HaveCount(3);
        waits.All.Should().Equal(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        destination.Length.Should().Be(0);
    }

    [Theory]
    [InlineData("0", 0, 1)]
    [InlineData("0", 1, 2)]
    [InlineData("2", 0, 2)]
    [InlineData("2", 1, 3)]
    [InlineData("30", 0, 30)]
    [InlineData("30", 1, 36)]
    [InlineData("60", 1, 72)]
    public async Task TestPriceFileServiceUnavailableWaitIsSpread(string retryAfter, double jitter, int waitSeconds)
    {
        using var server = new PricesServer((n, request) => n == 1
            ? PricesServer.ServingFailure(retryAfter)
            : PricesServer.PriceFile(request, Fixture));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, jitter);

        await prices.HeadAsync();

        waits.All.Should().Equal(TimeSpan.FromSeconds(waitSeconds));
    }

    [Theory]
    [InlineData("120", 120)]
    [InlineData("Wed, 21 Oct 2026 07:28:00 GMT", null)]
    [InlineData(null, null)]
    public async Task TestPriceFileDownloadServiceUnavailableNotRetried(string? retryAfter, int? retryAfterSeconds)
    {
        using var server = new PricesServer((_, _) => PricesServer.ServingFailure(retryAfter));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, 0);

        var act = () => prices.DownloadAsync(Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ServiceUnavailable);
        e.RetryAfter.Should().Be(retryAfterSeconds.HasValue ? TimeSpan.FromSeconds(retryAfterSeconds.Value) : (TimeSpan?)null);
        server.Requests.Should().ContainSingle();
        waits.All.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileDownloadWaitCancelled()
    {
        using var server = new PricesServer((_, _) => PricesServer.ServingFailure("1"));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        using var cts = new CancellationTokenSource();
        prices.Delay = (wait, cancellationToken) =>
        {
            cts.Cancel();
            return Task.Delay(wait, cancellationToken);
        };

        var act = () => prices.DownloadAsync(Stream.Null, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        server.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task TestPriceFileDownloadTooManyRequestsNotRetried()
    {
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(429, "", "30"));
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var waits = RecordedWaits.Stub(prices, 0);

        var act = () => prices.DownloadAsync(Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.TooManyRequests);
        e.StatusCode.Should().Be(429);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        server.Requests.Should().ContainSingle();
        waits.All.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileDownloadRetriesRefusedTokenOnce()
    {
        using var server = new PricesServer((n, request) => n == 1
            ? PricesServer.JsonApi(401, UnauthorizedBody)
            : PricesServer.PriceFile(request, Fixture));
        var client = server.NewClient();
        using var destination = new MemoryStream();

        await client.VoiceInPrices().DownloadAsync(destination);

        destination.ToArray().Should().Equal(Fixture, "only the file bytes are written");
        server.Requests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
        server.MintAudiences().Should().Equal("api-vin.didww.com", "api-vin.didww.com");
    }

    [Fact]
    public async Task TestPriceFileDownloadToFileNotAuthorized()
    {
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(401, UnauthorizedBody));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().DownloadToFileAsync(_dir.File("voice_in_prices.csv.gz"));

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.NotAuthorized);
        e.StatusCode.Should().Be(401);
        e.Code.Should().Be("401");
        e.Message.Should().Be("Prices service: Authorization failed");
        _dir.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileDownloadPricePlanNotAssigned()
    {
        var body = FakeAuthServer.LoadFixture("prices/errors/price_plan_not_assigned.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(404, body));
        var client = server.NewClient();
        using var destination = new MemoryStream();

        var act = () => client.VoiceInPrices().DownloadAsync(destination);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.PricePlanNotAssigned);
        e.Message.Should().Be("Prices service: No price plan is assigned to this account.");
        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task TestPriceFileDownloadAccessDenied()
    {
        using var server = new PricesServer((_, _) =>
            PricesServer.JsonApi(403, """{"errors":[{"title":"Insufficient scope","code":"403","status":"403"}]}"""));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().DownloadAsync(Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.AccessDenied);
        e.Message.Should().Be("Prices service: Insufficient scope");
        server.Requests.Should().ContainSingle("a 403 is never retried");
        server.Auth.Mints.Should().Be(1);
    }

    [Theory]
    [InlineData(202)]
    [InlineData(204)]
    [InlineData(302)]
    public async Task TestPriceFileDownloadUnexpectedStatus(int status)
    {
        using var server = new PricesServer((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)status);
            response.Headers.Location = new Uri("https://vin.example.test/v3/elsewhere.csv.gz");
            return response;
        });
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().DownloadToFileAsync(_dir.File("voice_in_prices.csv.gz"));

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.UnexpectedStatus);
        e.StatusCode.Should().Be(status);
        e.Message.Should().Be($"Prices service responded with status {status}");
        server.Requests.Should().ContainSingle("redirects are not followed");
        _dir.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileDownloadDestinationError()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        var writeError = new IOException("disk full");

        var act = () => client.VoiceInPrices().DownloadAsync(new ThrowingStream(writeError));

        (await act.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(writeError,
            "the destination's exception propagates as is");
    }

    [Fact]
    public async Task TestPriceFileDownloadBrokenBody()
    {
        var readError = new IOException("connection reset");
        using var server = new PricesServer((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ThrowingStream(readError))
        });
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        WriteOldCopy(path);

        var act = () => client.VoiceInPrices().DownloadToFileAsync(path);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().Be(200);
        e.InnerException.Should().BeSameAs(readError);
        File.ReadAllBytes(path).Should().Equal(OldCopy);
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }

    [Fact]
    public async Task TestPriceFileDownloadConnectionError()
    {
        using var server = new PricesServer((_, _) => throw new HttpRequestException("connection refused"));
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        WriteOldCopy(path);

        var act = () => client.VoiceInPrices().DownloadToFileAsync(path);

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().BeNull();
        File.ReadAllBytes(path).Should().Equal(OldCopy);
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }

    [Fact]
    public async Task TestPriceFileDownloadToFile()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        WriteOldCopy(path);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var file = await client.VoiceInPrices().DownloadToFileAsync(path);

        File.ReadAllBytes(path).Should().Equal(Fixture);
        File.GetLastWriteTimeUtc(path).Should().Be(PricesServer.LastModified.UtcDateTime, "modification time is Last-Modified");
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
        file.StatusCode.Should().Be(200);
        file.BytesWritten.Should().Be(Fixture.Length);
        file.FileName.Should().Be("voice_in_prices.csv.gz");
        if (!OperatingSystem.IsWindows())
            File.GetUnixFileMode(path).Should().Be(NewFileMode(), "mode of a new file, not of the old copy");
    }

    [Fact]
    public async Task TestPriceFileDownloadToFileLeavesOtherTempFiles()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        File.WriteAllText(path + ".tmp", "concurrent download");

        await client.VoiceInPrices().DownloadToFileAsync(path);

        File.ReadAllBytes(path).Should().Equal(Fixture);
        File.ReadAllText(path + ".tmp").Should().Be("concurrent download");
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz", "voice_in_prices.csv.gz.tmp");
    }

    [Fact]
    public async Task TestPriceFileDownloadToFileErrorKeepsExistingFile()
    {
        var body = FakeAuthServer.LoadFixture("prices/errors/price_plan_not_assigned.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(404, body));
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        WriteOldCopy(path);

        var act = () => client.VoiceInPrices().DownloadToFileAsync(path);

        (await act.Should().ThrowAsync<DidwwPricesException>()).Which.Kind
            .Should().Be(DidwwPricesErrorKind.PricePlanNotAssigned);
        File.ReadAllBytes(path).Should().Equal(OldCopy);
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }

    [Fact]
    public async Task TestPriceFileDownloadToFileMissingDirectory()
    {
        using var server = ServeFile();
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().DownloadToFileAsync(Path.Combine(_dir.Path, "missing", "prices.csv.gz"));

        await act.Should().ThrowAsync<DirectoryNotFoundException>();
        server.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPriceFileFetchWithoutCopy()
    {
        using var server = ServeFile();
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");

        var file = await client.VoiceInPrices().FetchAsync(path);

        file.StatusCode.Should().Be(200);
        File.ReadAllBytes(path).Should().Equal(Fixture);
        server.Requests.Single().Headers.Should().NotContainKey("If-Modified-Since");
    }

    [Fact]
    public async Task TestPriceFileFetchSendsModificationTime()
    {
        using var server = new PricesServer((_, _) => PricesServer.NotModified());
        var client = server.NewClient();
        var path = _dir.File("voice_in_prices.csv.gz");
        WriteOldCopy(path, PricesServer.LastModified);

        var file = await client.VoiceInPrices().FetchAsync(path);

        file.NotModified.Should().BeTrue();
        server.Requests.Single().Headers["If-Modified-Since"].Should().Be(PricesServer.LastModifiedHeader);
        File.ReadAllBytes(path).Should().Equal(OldCopy);
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }

    [Fact]
    public async Task TestPriceFileFetchReplacesChangedCopy()
    {
        var previous = Encoding.ASCII.GetBytes("previous copy");
        using var server = new PricesServer((n, request) => n switch
        {
            1 => PricesServer.PriceFile(request, previous),
            2 => PricesServer.PriceFile(request, Fixture, PricesServer.OtherLastModifiedHeader),
            _ => PricesServer.NotModified(PricesServer.OtherLastModifiedHeader)
        });
        var client = server.NewClient();
        var prices = client.VoiceInPrices();
        var path = _dir.File("voice_in_prices.csv.gz");

        for (var i = 0; i < 3; i++)
            await prices.FetchAsync(path);

        var requests = server.Requests;
        requests.Should().HaveCount(3);
        requests[0].Headers.Should().NotContainKey("If-Modified-Since");
        requests[1].Headers["If-Modified-Since"].Should().Be(PricesServer.LastModifiedHeader);
        requests[2].Headers["If-Modified-Since"].Should().Be(PricesServer.OtherLastModifiedHeader);
        requests.Should().AllSatisfy(r => r.Headers.Should().NotContainKey("If-None-Match"));
        File.ReadAllBytes(path).Should().Equal(Fixture);
        File.GetLastWriteTimeUtc(path).Should().Be(PricesServer.OtherLastModified.UtcDateTime);
        _dir.Entries().Should().Equal("voice_in_prices.csv.gz");
    }
}
