using Didww.Api3.Exception;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class EncryptedFileTest : BaseTest
{
    [Fact]
    public async Task TestListEncryptedFiles()
    {
        StubGet("encrypted_files", "encrypted_files/index.json");

        var response = await Client.EncryptedFiles().ListAsync();
        var files = response.Data;

        files.Should().NotBeEmpty();

        var first = files[0];
        first.Id.Should().Be("7f2fbdca-8008-44ce-bcb6-3537ea5efaac");
        first.Description.Should().Be("file.enc");
        first.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task TestFindEncryptedFile()
    {
        StubGet("encrypted_files/6eed102c-66a9-4a9b-a95f-4312d70ec12a", "encrypted_files/show.json");

        var response = await Client.EncryptedFiles().FindAsync("6eed102c-66a9-4a9b-a95f-4312d70ec12a");
        var file = response.Data;

        file.Id.Should().Be("6eed102c-66a9-4a9b-a95f-4312d70ec12a");
        file.Description.Should().Be("some description");
    }

    [Fact]
    public async Task TestDeleteEncryptedFile()
    {
        var id = "6eed102c-66a9-4a9b-a95f-4312d70ec12a";
        StubDelete("encrypted_files/" + id);

        await Client.EncryptedFiles().DeleteAsync(id);
    }

    [Fact]
    public async Task TestUploadEncryptedFile()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"id":"f6a7b890-1234-5678-9abc-def123456789","type":"encrypted_files","attributes":{"description":"Test description","expires_at":"2026-04-22T10:00:00.000Z"}}}""")
        );

        var fileData = "test file data"u8.ToArray();
        var result = await Client.UploadEncryptedFileAsync(
            fileData, "test.pdf", "fingerprint123", "Test description");

        result.Should().Be("f6a7b890-1234-5678-9abc-def123456789");
    }

    [Fact]
    public async Task TestUploadEncryptedFileUsesTheBaseUrlAsGiven()
    {
        WireMock.Given(
            Request.Create().WithPath("/didww/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"id":"f6a7b890-1234-5678-9abc-def123456789","type":"encrypted_files","attributes":{}}}""")
        );
        var client = NewClientBuilder().SetBaseUrl(WireMock.Url + "/didww").Build();

        var result = await client.UploadEncryptedFileAsync("test"u8.ToArray(), "test.pdf", "fp123");

        result.Should().Be("f6a7b890-1234-5678-9abc-def123456789");
    }

    [Fact]
    public async Task TestUploadEncryptedFileWithoutDescription()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"id":"f6a7b890-1234-5678-9abc-def123456789","type":"encrypted_files","attributes":{"description":"test.pdf","expires_at":"2026-04-22T10:00:00.000Z"}}}""")
        );

        var fileData = "test"u8.ToArray();
        var result = await Client.UploadEncryptedFileAsync(fileData, "test.pdf", "fp123");

        result.Should().Be("f6a7b890-1234-5678-9abc-def123456789");
    }

    [Fact]
    public async Task TestUploadEncryptedFileHttpError()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithBody("Validation failed")
        );

        var act = () => Client.UploadEncryptedFileAsync(
            "data"u8.ToArray(), "test.pdf", "fp123");

        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*422*");
    }

    [Fact]
    public async Task TestUploadIncludesUserAgentHeader()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":{"id":"file-id-1","type":"encrypted_files","attributes":{"description":"test.pdf","expires_at":"2026-04-22T10:00:00.000Z"}}}""")
        );

        var fileData = "test"u8.ToArray();
        await Client.UploadEncryptedFileAsync(fileData, "test.pdf", "fp123");

        var request = RequestsTo("/v3/encrypted_files").Should().ContainSingle().Subject;
        request.Headers!["User-Agent"].First().Should().Be(DidwwClient.SdkUserAgent);
        request.Headers!["Authorization"].Should().ContainSingle().Which.Should().Be("Bearer " + TestAccessToken);
        request.Headers!["Accept"].Should().ContainSingle().Which.Should().Be("application/json");
        request.Headers.Should().NotContainKey("Api-Key");
        request.Headers.Should().NotContainKey("X-DIDWW-API-Version");
    }

    [Fact]
    public async Task TestUploadEncryptedFileRetriedAfter401()
    {
        using var server = new FakeAuthServer();
        server.Api = (r, _) => Task.FromResult(r.Authorization == "Bearer token-1"
            ? new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            : FakeAuthServer.Json(201,
                """{"data":{"id":"6eed102c-66a9-4a9b-a95f-4312d70ec12a","type":"encrypted_files","attributes":{"description":"sample.pdf"}}}"""));
        var client = server.NewClient();

        var id = await client.UploadEncryptedFileAsync("encrypted-content"u8.ToArray(), "sample.pdf.enc",
            "fingerprint-123", "sample.pdf");

        id.Should().Be("6eed102c-66a9-4a9b-a95f-4312d70ec12a");
        var requests = server.ApiRequests;
        requests.Should().HaveCount(2);
        requests[0].Uri.AbsolutePath.Should().Be("/v3/encrypted_files");
        requests[0].Headers["Content-Type"].Should().StartWith("multipart/form-data");
        requests[0].Body.Should().Contain("encrypted-content").And.Contain("fingerprint-123").And.Contain("sample.pdf.enc");
        requests[1].Body.Should().Be(requests[0].Body);
        requests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
        server.Mints.Should().Be(2);
    }

    [Fact]
    public async Task TestUploadEncryptedFileSecond401()
    {
        using var server = new FakeAuthServer();
        server.Api = (_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
        var client = server.NewClient();

        var act = () => client.UploadEncryptedFileAsync("encrypted-content"u8.ToArray(), "sample.pdf.enc",
            "fingerprint-123", "sample.pdf");

        await act.Should().ThrowAsync<DidwwClientException>().WithMessage("*HTTP 401*");
        server.ApiRequests.Should().HaveCount(2);
        server.Mints.Should().Be(2);
    }

    [Fact]
    public async Task TestUploadEncryptedFileUnexpectedResponse()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/encrypted_files").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":"unexpected"}""")
        );

        var act = () => Client.UploadEncryptedFileAsync(
            "data"u8.ToArray(), "test.pdf", "fp123");

        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*Unexpected*");
    }
}
