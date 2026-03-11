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
        first.ExpireAt.Should().NotBeNull();
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
                .WithBody("""{"ids":["file-id-1","file-id-2"]}""")
        );

        var fileData = "test file data"u8.ToArray();
        var result = await Client.UploadEncryptedFileAsync(
            fileData, "test.pdf", "fingerprint123", "Test description");

        result.Should().HaveCount(2);
        result[0].Should().Be("file-id-1");
        result[1].Should().Be("file-id-2");
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
                .WithBody("""{"ids":["file-id-1"]}""")
        );

        var fileData = "test"u8.ToArray();
        var result = await Client.UploadEncryptedFileAsync(fileData, "test.pdf", "fp123");

        result.Should().HaveCount(1);
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
                .WithBody("""{"ids":["file-id-1"]}""")
        );

        var fileData = "test"u8.ToArray();
        await Client.UploadEncryptedFileAsync(fileData, "test.pdf", "fp123");

        var request = WireMock.LogEntries.First().RequestMessage;
        request.Headers!["User-Agent"].First().Should().Be(DidwwClient.SdkUserAgent);
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
