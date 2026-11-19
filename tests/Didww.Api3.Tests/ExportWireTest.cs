using Didww.Api3.Auth;
using Didww.Api3.Exception;
using FluentAssertions;

namespace Didww.Api3.Tests;

/// <summary>
/// Export downloads over real sockets: the token service is WireMock, the API
/// host a raw HTTP server that sends exactly the bytes given.
/// </summary>
public class ExportWireTest : BaseTest
{
    [Fact]
    public async Task TestDownloadExportReportsStatusWhenErrorBodyBreaksOff()
    {
        using var api = new RawHttpServer((_, _) =>
            "HTTP/1.1 404 Not Found\r\n" +
            "Content-Type: application/vnd.api+json\r\n" +
            "Content-Length: 200\r\n" +
            "Connection: close\r\n\r\n" +
            "{\"errors\":");
        var client = NewClientBuilder().SetBaseUrl(api.Url + "/v3").Build();
        using var destination = new MemoryStream();

        var act = () => client.DownloadExportAsync(api.Url + "/v3/exports/export.csv.gz", destination);

        (await act.Should().ThrowAsync<DidwwApiException>()).Which.HttpStatus.Should().Be(404);
        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task TestDownloadExportReportsStatusWhenErrorBodyStalls()
    {
        using var api = new RawHttpServer((_, _) =>
            "HTTP/1.1 404 Not Found\r\n" +
            "Content-Type: application/vnd.api+json\r\n" +
            "Content-Length: 200\r\n\r\n" +
            "{\"errors\":[", hold: true);
        var client = NewClientBuilder().SetBaseUrl(api.Url + "/v3").SetTimeout(TimeSpan.FromSeconds(1)).Build();
        await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None); // the 1 s covers the API host only
        using var destination = new MemoryStream();

        var act = () => client.DownloadExportAsync(api.Url + "/v3/exports/export.csv.gz", destination)
            .WaitAsync(TimeSpan.FromSeconds(15));

        (await act.Should().ThrowAsync<DidwwApiException>()).Which.HttpStatus.Should().Be(404);
        destination.Length.Should().Be(0);
    }
}
