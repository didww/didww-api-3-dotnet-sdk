using Didww.Api3.Exception;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class TooManyRequestsTest : BaseTest
{
    private const string TooManyRequestsBody =
        """{"errors":[{"title":"Too many requests","detail":"The request budget is spent","code":"too_many_requests","status":"429"}]}""";

    private const string DidId = "9df99644-f1a5-4a3c-99a4-559d758eb96b";

    private static readonly string[] OperationNames = ["api", "write", "export", "upload"];

    public static TheoryData<string> Operations => new(OperationNames);

    public static TheoryData<string, string?, int?> RetryAfterCases()
    {
        var cases = new TheoryData<string, string?, int?>();
        foreach (var operation in OperationNames)
        {
            cases.Add(operation, "30", 30);
            cases.Add(operation, null, null);
            cases.Add(operation, "1.5", null);
            cases.Add(operation, "Wed, 21 Oct 2026 07:28:00 GMT", null);
        }
        return cases;
    }

    private (string Path, Func<Task> Call) Operation(string name) => name switch
    {
        "api" => ("/v3/dids/" + DidId, () => Client.Dids().FindAsync(DidId)),
        "write" => ("/v3/dids/" + DidId, () => Client.Dids().DeleteAsync(DidId)),
        "export" => ("/v3/exports/02bf6df4.csv.gz",
            () => Client.DownloadExportAsync(WireMock.Url + "/v3/exports/02bf6df4.csv.gz", Stream.Null)),
        "upload" => ("/v3/encrypted_files",
            () => Client.UploadEncryptedFileAsync("encrypted-content"u8.ToArray(), "sample.pdf.enc", "fingerprint-123")),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private void Stub(string path, int status, string body, string? retryAfter, string contentType = "application/vnd.api+json")
    {
        var response = Response.Create().WithStatusCode(status).WithHeader("Content-Type", contentType).WithBody(body);
        if (retryAfter != null)
            response = response.WithHeader("Retry-After", retryAfter);
        WireMock.Given(Request.Create().WithPath(path)).RespondWith(response);
    }

    [Theory]
    [MemberData(nameof(RetryAfterCases))]
    public async Task TestTooManyRequestsCarriesRetryAfter(string operation, string? retryAfter, int? seconds)
    {
        var (path, call) = Operation(operation);
        Stub(path, 429, TooManyRequestsBody, retryAfter);

        var e = (await call.Should().ThrowAsync<DidwwApiException>()).Which;

        e.HttpStatus.Should().Be(429);
        e.RetryAfter.Should().Be(seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : null);
        e.Errors.Should().ContainSingle().Which.Code.Should().Be("too_many_requests");
        e.Message.Should().NotContain(TestAccessToken).And.NotContain("test-api-key");
        RequestsTo(path).Should().HaveCount(1, "a 429 is not retried");
        MintCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task TestTooManyRequestsWithoutErrorsDocument(string operation)
    {
        var (path, call) = Operation(operation);
        Stub(path, 429, "Too Many Requests", "7", "text/plain");

        var e = (await call.Should().ThrowAsync<DidwwApiException>()).Which;

        e.HttpStatus.Should().Be(429);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
        e.Message.Should().Contain("Too Many Requests");
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task TestRetryAfterOnOtherErrorStatus(string operation)
    {
        var (path, call) = Operation(operation);
        Stub(path, 503, """{"errors":[{"title":"Service unavailable","status":"503"}]}""", "30");

        var e = (await call.Should().ThrowAsync<DidwwApiException>()).Which;

        e.HttpStatus.Should().Be(503);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        RequestsTo(path).Should().HaveCount(1);
    }
}
