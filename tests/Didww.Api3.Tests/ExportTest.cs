using System.IO.Compression;
using Didww.Api3.Exception;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class ExportTest : BaseTest
{
    [Fact]
    public void TestStatusHelperPending()
    {
        var export = new Export { Status = ExportStatus.Pending };
        export.IsPending.Should().BeTrue();
        export.IsProcessing.Should().BeFalse();
        export.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void TestStatusHelperProcessing()
    {
        var export = new Export { Status = ExportStatus.Processing };
        export.IsProcessing.Should().BeTrue();
        export.IsPending.Should().BeFalse();
        export.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void TestStatusHelperCompleted()
    {
        var export = new Export { Status = ExportStatus.Completed };
        export.IsCompleted.Should().BeTrue();
        export.IsPending.Should().BeFalse();
        export.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task TestListExports()
    {
        StubGet("exports", "exports/index.json");

        var response = await Client.Exports().ListAsync();
        var exports = response.Data;

        exports.Should().NotBeEmpty();

        var first = exports[0];
        first.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        first.ExportType.Should().Be(ExportType.CdrIn);
        first.Status.Should().Be(ExportStatus.Completed);
    }

    [Fact]
    public async Task TestFindExport()
    {
        StubGet("exports/da15f006-5da4-45ca-b0df-735baeadf423", "exports/show.json");

        var response = await Client.Exports().FindAsync("da15f006-5da4-45ca-b0df-735baeadf423");
        var export = response.Data;

        export.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        export.ExportType.Should().Be(ExportType.CdrIn);
        export.Status.Should().Be(ExportStatus.Completed);
    }

    [Fact]
    public async Task TestCreateExport()
    {
        StubPost("exports", "exports/create_request.json", "exports/create.json");

        var export = new Export
        {
            ExportType = ExportType.CdrIn,
            Filters = new Dictionary<string, object>
            {
                { "did_number", "1234556789" },
                { "from", "2026-04-01 00:00:00" },
                { "to", "2026-04-15 23:59:59" }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        var created = response.Data;

        created.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        created.ExportType.Should().Be(ExportType.CdrIn);
        created.Status.Should().Be(ExportStatus.Pending);
    }

    [Fact]
    public async Task TestCreateCdrOutExport()
    {
        StubPost("exports", "exports/create_cdr_out.json");

        var export = new Export
        {
            ExportType = ExportType.CdrOut,
            Filters = new Dictionary<string, object>
            {
                { "from", "2026-04-01 00:00:00" },
                { "to", "2026-04-15 23:59:59" }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        var created = response.Data;

        created.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        created.ExportType.Should().Be(ExportType.CdrOut);
        created.Status.Should().Be(ExportStatus.Pending);
    }

    [Fact]
    public async Task TestUpdateExportExternalReferenceId()
    {
        StubPatch("exports/21e02b15-806d-44b3-b67f-434ea6c44f61",
            "exports/update_external_reference_id_request.json",
            "exports/update_external_reference_id.json");

        var export = Export.Build("21e02b15-806d-44b3-b67f-434ea6c44f61");
        export.ExternalReferenceId = "renamed-ref-99";

        var response = await Client.Exports().UpdateAsync(export);
        var updated = response.Data;

        updated.Id.Should().Be("21e02b15-806d-44b3-b67f-434ea6c44f61");
        updated.ExternalReferenceId.Should().Be("renamed-ref-99");
    }

    [Fact]
    public async Task TestDownloadExport()
    {
        var csvContent = "Date/Time Start (UTC),DID,Duration\n2018-12-06,972397239159652,0\n";
        var gzData = CompressToGzip(csvContent);

        var exportUrl = WireMock.Url + "/v3/exports/02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/octet-stream")
                .WithHeader("Content-Disposition", "attachment; filename=\"02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz\"")
                .WithBody(gzData)
        );

        var export = new Export { Id = "02bf6df4-3af9-416c-96be-16e5b7eeb651", Url = exportUrl };
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await Client.DownloadExportAsync(export, tempFile);
            var bytes = await File.ReadAllBytesAsync(tempFile);
            // Verify gzip magic bytes
            bytes[0].Should().Be(0x1f);
            bytes[1].Should().Be(0x8b);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task TestDownloadAndDecompressExport()
    {
        var csvContent = "Date/Time Start (UTC),DID,Duration\n2018-12-06,972397239159652,0\n";
        var gzData = CompressToGzip(csvContent);

        var exportUrl = WireMock.Url + "/v3/exports/02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/octet-stream")
                .WithHeader("Content-Disposition", "attachment; filename=\"02bf6df4-3af9-416c-96be-16e5b7eeb651.csv.gz\"")
                .WithBody(gzData)
        );

        var export = new Export { Id = "02bf6df4-3af9-416c-96be-16e5b7eeb651", Url = exportUrl };
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await Client.DownloadAndDecompressExportAsync(export, tempFile);
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("Date/Time Start (UTC)");
            content.Should().Contain("972397239159652");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task TestDownloadExportNullUrl()
    {
        var export = new Export { Id = "test-id", Url = null };

        var act = () => Client.DownloadExportAsync(export, Path.Combine(Path.GetTempPath(), "test.csv"));
        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*URL is null*");
    }

    [Fact]
    public async Task TestDownloadExportHttpError()
    {
        var exportUrl = WireMock.Url + "/v3/exports/nonexistent.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/nonexistent.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(404)
        );

        var export = new Export { Id = "nonexistent", Url = exportUrl };
        var filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var act = () => Client.DownloadExportAsync(export, filePath);
        var e = (await act.Should().ThrowAsync<DidwwApiException>().WithMessage("*HTTP 404*")).Which;
        e.HttpStatus.Should().Be(404);
        File.Exists(filePath).Should().BeFalse("the file is created only after a 200 answer");
    }

    [Fact]
    public async Task TestDownloadExportSendsRequiredHeaders()
    {
        var exportUrl = WireMock.Url + "/v3/exports/test-id.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/test-id.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/octet-stream")
                .WithBody(new byte[] { 0x1f, 0x8b })
        );

        var export = new Export { Id = "test-id", Url = exportUrl };
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            await Client.DownloadExportAsync(export, tempFile);

            var request = RequestsTo("/v3/exports/test-id.csv.gz").Should().ContainSingle().Subject;
            request.Headers!["Authorization"].Should().ContainSingle()
                .Which.Should().Be("Bearer " + TestAccessToken);
            request.Headers.Should().NotContainKey("Api-Key");
            request.Headers.Should().NotContainKey("X-DIDWW-API-Version");
            request.Headers!["User-Agent"].Should().ContainSingle()
                .Which.Should().Be(DidwwClient.SdkUserAgent);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task TestDownloadExportToStream()
    {
        var gzData = CompressToGzip("Date/Time Start (UTC),DID\n");
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithHeader("Content-Type", "application/octet-stream").WithBody(gzData)
        );
        using var destination = new MemoryStream();

        await Client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz", destination);

        destination.ToArray().Should().Equal(gzData);
        MintCount.Should().Be(1);
    }

    [Fact]
    public async Task TestDownloadExportJsonApiError()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/missing.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("""{"errors":[{"title":"Not found","detail":"Export file not found","status":"404"}]}""")
        );

        var act = () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/missing.csv.gz", Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwApiException>()).Which;
        e.HttpStatus.Should().Be(404);
        e.Errors.Should().ContainSingle().Which.Detail.Should().Be("Export file not found");
    }

    [Fact]
    public async Task TestDownloadExportPlainError()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/missing.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(404).WithBody("not found")
        );

        var act = () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/missing.csv.gz", Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwApiException>()).Which;
        e.Errors.Should().ContainSingle().Which.Detail.Should().Be("not found");
    }

    [Fact]
    public async Task TestDownloadExportDoesNotFollowRedirects()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(302).WithHeader("Location", WireMock.Url + "/elsewhere/export.csv.gz")
        );
        WireMock.Given(
            Request.Create().WithPath("/elsewhere/export.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(CompressToGzip("x"))
        );
        using var destination = new MemoryStream();

        var act = () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz", destination);

        (await act.Should().ThrowAsync<DidwwApiException>()).Which.HttpStatus.Should().Be(302);
        RequestsTo("/elsewhere/export.csv.gz").Should().BeEmpty();
        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task TestDownloadExportRefusesRedirectFollowedByInnerHandler()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(302).WithHeader("Location", WireMock.Url + "/elsewhere/export.csv.gz")
        );
        WireMock.Given(
            Request.Create().WithPath("/elsewhere/export.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(CompressToGzip("x"))
        );
        using var followingHandler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = NewClientBuilder().SetInnerHandler(followingHandler).Build();
        using var destination = new MemoryStream();

        var act = () => client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz", destination);

        (await act.Should().ThrowAsync<DidwwClientException>()).Which.Message.Should().Contain("redirected");
        RequestsTo("/elsewhere/export.csv.gz").Should().ContainSingle("the inner handler followed the redirect");
        destination.Length.Should().Be(0, "nothing from the redirect target is written");
    }

    [Fact]
    public async Task TestDownloadExportNoContent()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(204)
        );

        var act = () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz", Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwApiException>()).Which;
        e.HttpStatus.Should().Be(204);
        e.Errors.Should().ContainSingle().Which.Title.Should().Be("Unexpected status");
    }

    [Fact]
    public async Task TestDownloadExportRetriedAfter401()
    {
        var gzData = CompressToGzip("Date/Time Start (UTC),DID\n");
        using var server = new FakeAuthServer();
        server.Api = (r, _) =>
        {
            if (r.Authorization == "Bearer token-1")
                return Task.FromResult(FakeAuthServer.Json(401, "{}", "application/vnd.api+json"));
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(gzData)
            });
        };
        var client = server.NewClient();
        using var destination = new MemoryStream();

        await client.DownloadExportAsync("https://api.example.test/v3/exports/02bf6df4.csv.gz", destination);

        destination.ToArray().Should().Equal(gzData);
        server.ApiRequests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
        server.Mints.Should().Be(2);
    }

    [Theory]
    [InlineData("other host", "https://other.example.test/v3/exports/1.csv.gz")]
    [InlineData("http on an https base URL", "http://api.example.test/v3/exports/1.csv.gz")]
    [InlineData("relative URL", "/v3/exports/1.csv.gz")]
    [InlineData("not a URL", "::")]
    public async Task TestDownloadExportRefusesUrlOutsideBaseUrl(string name, string url)
    {
        using var server = new FakeAuthServer();
        var client = server.NewClient();
        using var destination = new MemoryStream();

        var toStream = () => client.DownloadExportAsync(url, destination);
        var decompressed = () => client.DownloadAndDecompressExportAsync(new Export { Id = "1", Url = url },
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        (await toStream.Should().ThrowAsync<DidwwClientException>()).Which.Message
            .Should().Contain("does not match the API base URL", name);
        (await decompressed.Should().ThrowAsync<DidwwClientException>()).Which.Message
            .Should().Contain("does not match the API base URL", name);
        destination.Length.Should().Be(0);
        server.ApiRequests.Should().BeEmpty();
        server.Mints.Should().Be(0);
    }

    [Theory]
    [InlineData("https://proxy.example.com/v3", "https://proxy.example.com/v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com/v3", "https://api.didww.com/v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com/v3", "https://api.didww.com:443/v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com/v3", "https://API.didww.com./v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com:443/v3", "https://proxy.example.com/v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com./v3", "https://proxy.example.com/v3/exports/1.csv.gz", true)]
    [InlineData("http://proxy.example.com/v3", "http://proxy.example.com:80/v3/exports/1.csv.gz", true)]
    [InlineData("https://proxy.example.com/v3", "https://other.example.com/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "https://sandbox-api.didww.com/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "http://api.didww.com/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "https://api.didww.com:8443/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "https://proxy.example.com:8443/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "http://proxy.example.com:443/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "/v3/exports/1.csv.gz", false)]
    [InlineData("https://proxy.example.com/v3", "file:///v3/exports/1.csv.gz", false)]
    public void TestCheckExportUrlOrigin(string baseUrl, string url, bool allowed)
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-api-key", DidwwEnvironment.Production))
            .SetBaseUrl(baseUrl)
            .Build();

        var act = () => client.CheckExportUrl(url);

        if (allowed)
            act.Should().NotThrow();
        else
            act.Should().Throw<DidwwClientException>().WithMessage("*does not match the API base URL*");
    }

    [Fact]
    public async Task TestDownloadExportPropagatesDestinationError()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/02bf6df4.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBody(CompressToGzip("Date/Time Start (UTC),DID\n"))
        );
        var writeError = new IOException("disk full");

        var act = () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz",
            new FailingStream(writeError));

        (await act.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(writeError);
    }

    [Fact]
    public async Task TestDownloadExportWrapsBrokenBody()
    {
        using var server = new FakeAuthServer();
        server.Api = (_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StreamContent(new FailingStream(new IOException("connection reset")))
        });
        var client = server.NewClient();

        var act = () => client.DownloadExportAsync("https://api.example.test/v3/exports/1.csv.gz", Stream.Null);

        var e = (await act.Should().ThrowAsync<DidwwClientException>()).Which;
        e.Message.Should().Contain("Failed to read export data");
        e.InnerException.Should().BeOfType<IOException>();
    }

    private sealed class FailingStream : Stream
    {
        private readonly System.Exception _error;

        public FailingStream(System.Exception error)
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

    private static byte[] CompressToGzip(string content)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            gz.Write(System.Text.Encoding.UTF8.GetBytes(content));
        }
        return ms.ToArray();
    }
}
