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
                { "year", "2019" },
                { "month", "01" }
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
                { "year", 2024 },
                { "month", 1 }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        var created = response.Data;

        created.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        created.ExportType.Should().Be(ExportType.CdrOut);
        created.Status.Should().Be(ExportStatus.Pending);
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
        var tempFile = Path.GetTempFileName();
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
        var tempFile = Path.GetTempFileName();
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
        var act = () => Client.DownloadExportAsync(export, Path.Combine(Path.GetTempPath(), "test.csv"));
        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*404*");
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
        var tempFile = Path.GetTempFileName();
        try
        {
            await Client.DownloadExportAsync(export, tempFile);

            var request = WireMock.LogEntries.First().RequestMessage;
            request.Headers.Should().ContainKey("Api-Key");
            request.Headers.Should().ContainKey("User-Agent");
            request.Headers.Should().ContainKey(DidwwClient.ApiVersionHeader);
            request.Headers!["User-Agent"].Should().ContainSingle()
                .Which.Should().Be(DidwwClient.SdkUserAgent);
        }
        finally
        {
            File.Delete(tempFile);
        }
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
