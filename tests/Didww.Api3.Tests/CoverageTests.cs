using System.IO.Compression;
using System.Net;
using Didww.Api3.Callback;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;
using Didww.Api3.Resource.OrderItem;
using FluentAssertions;
using JsonApiSerializer;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

// --- UploadEncryptedFileAsync tests ---
public class UploadEncryptedFileTest : BaseTest
{
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

// --- DownloadExportAsync tests ---
public class DownloadExportTest : BaseTest
{
    [Fact]
    public async Task TestDownloadExportToFile()
    {
        var exportUrl = WireMock.Url + "/v3/exports/test-id.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/test-id.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithBody("col1,col2\nval1,val2\n")
        );

        var export = new Export { Id = "test-id", Url = exportUrl };
        var tempFile = Path.GetTempFileName();
        try
        {
            await Client.DownloadExportAsync(export, tempFile);
            File.ReadAllText(tempFile).Should().Contain("col1,col2");
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

        var act = () => Client.DownloadExportAsync(export, "/tmp/test.csv");
        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*URL is null*");
    }

    [Fact]
    public async Task TestDownloadExportHttpError()
    {
        var exportUrl = WireMock.Url + "/v3/exports/test-id.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/test-id.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create().WithStatusCode(404)
        );

        var export = new Export { Id = "test-id", Url = exportUrl };
        var act = () => Client.DownloadExportAsync(export, "/tmp/test.csv");
        await act.Should().ThrowAsync<DidwwClientException>()
            .WithMessage("*404*");
    }
    [Fact]
    public async Task TestDownloadAndDecompressExport()
    {
        var csvContent = "Date/Time Start (UTC),DID,Duration\n2018-12-06,972397239159652,0\n";
        using var ms = new MemoryStream();
        await using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
        {
            await gz.WriteAsync(System.Text.Encoding.UTF8.GetBytes(csvContent));
        }
        var gzData = ms.ToArray();

        var exportUrl = WireMock.Url + "/v3/exports/test-id.csv.gz";
        WireMock.Given(
            Request.Create().WithPath("/v3/exports/test-id.csv.gz").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/gzip")
                .WithBody(gzData)
        );

        var export = new Export { Id = "test-id", Url = exportUrl };
        var tempFile = Path.GetTempFileName();
        try
        {
            await Client.DownloadAndDecompressExportAsync(export, tempFile);
            var content = File.ReadAllText(tempFile);
            content.Should().Contain("Date/Time Start (UTC)");
            content.Should().Contain("972397239159652");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

// --- VoiceOutTrunkRegenerateCredential tests ---
public class VoiceOutTrunkRegenerateCredentialTest : BaseTest
{
    [Fact]
    public async Task TestCreateVoiceOutTrunkRegenerateCredential()
    {
        StubPost("voice_out_trunk_regenerate_credentials",
            "voice_out_trunk_regenerate_credentials/create.json");

        var regen = VoiceOutTrunkRegenerateCredential.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        regen.VoiceOutTrunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");

        var response = await Client.VoiceOutTrunkRegenerateCredentials().CreateAsync(regen);
        response.Data.Id.Should().Be("5fc59e7e-79eb-498a-8779-800416b5c68a");
    }
}

// --- VoiceOutTrunk show with attributes test ---
public class VoiceOutTrunkShowTest : BaseTest
{
    [Fact]
    public async Task TestShowVoiceOutTrunk()
    {
        StubGet("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/show.json");

        var response = await Client.VoiceOutTrunks()
            .FindAsync("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        var trunk = response.Data;

        trunk.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.Username.Should().Be("dpjgwbbac9");
        trunk.Password.Should().Be("z0hshvbcy7");
        trunk.ThresholdReached.Should().BeFalse();
        trunk.Name.Should().Be("test");
        trunk.CapacityLimit.Should().Be(123);
    }
}

// --- StockKeepingUnit and QtyBasedPricing via DidGroup include ---
public class DidGroupShowTest : BaseTest
{
    [Fact]
    public async Task TestShowDidGroupWithStockKeepingUnits()
    {
        StubGet("did_groups/2187c36d-28fb-436f-8861-5a0f5b5a3ee1",
            "did_groups/show.json");

        var queryParams = new QueryParams().Include("stock_keeping_units", "country");
        var response = await Client.DidGroups()
            .FindAsync("2187c36d-28fb-436f-8861-5a0f5b5a3ee1", queryParams);
        var group = response.Data;

        group.Id.Should().Be("2187c36d-28fb-436f-8861-5a0f5b5a3ee1");
        group.Prefix.Should().Be("241");
        group.StockKeepingUnits.Should().NotBeNullOrEmpty();
        group.StockKeepingUnits.Should().HaveCount(2);

        var sku = group.StockKeepingUnits![0];
        sku.Id.Should().Be("5c6f00cd-cfca-441f-9322-5d000458b44f");
        sku.ChannelsIncludedCount.Should().Be(0);
    }
}

// --- Encrypt with client (fetches public keys) ---
public class EncryptWithClientTest : BaseTest
{
    [Fact]
    public async Task TestEncryptViaClient()
    {
        StubGet("public_keys", "public_keys/index.json");

        // Encrypt class calls ListAsync synchronously in constructor
        // We just need to test that it works
        var encrypt = new Encrypt(Client);
        encrypt.Fingerprint.Should().Contain(":::");
        encrypt.PublicKeys.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestEncryptResetAsync()
    {
        StubGet("public_keys", "public_keys/index.json");

        var encrypt = new Encrypt(Client);
        var fp1 = encrypt.Fingerprint;
        await encrypt.ResetAsync();
        encrypt.Fingerprint.Should().Be(fp1);
    }

    [Fact]
    public async Task TestEncryptDataViaClient()
    {
        StubGet("public_keys", "public_keys/index.json");

        var encrypt = new Encrypt(Client);
        var data = "test data"u8.ToArray();
        var encrypted = encrypt.EncryptData(data);
        encrypted.Should().NotBeEmpty();
        encrypted.Length.Should().BeGreaterThan(data.Length);
    }
}

// --- DidwwClientException tests ---
public class DidwwClientExceptionTest
{
    [Fact]
    public void TestConstructorWithMessage()
    {
        var ex = new DidwwClientException("something failed");
        ex.Message.Should().Be("something failed");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void TestConstructorWithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DidwwClientException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().Be(inner);
    }
}

// --- DidwwApiException additional coverage ---
public class DidwwApiExceptionTest
{
    [Fact]
    public void TestConstructorWithMessageOnly()
    {
        var ex = new DidwwApiException(503, "Service Unavailable");
        ex.HttpStatus.Should().Be(503);
        ex.Message.Should().Be("Service Unavailable");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TestApiErrorToString()
    {
        var error = new ApiError
        {
            Title = "validation error",
            Detail = "name is required"
        };
        error.ToString().Should().Contain("validation error");
        error.ToString().Should().Contain("name is required");
    }

    [Fact]
    public void TestMultipleErrors()
    {
        var errors = new List<ApiError>
        {
            new() { Title = "err1", Detail = "detail1" },
            new() { Title = "err2", Detail = "detail2" }
        };
        var ex = new DidwwApiException(422, errors);
        ex.Message.Should().Contain("detail1");
        ex.Message.Should().Contain("detail2");
        ex.Errors.Should().HaveCount(2);
    }
}

// --- RequestValidator edge cases ---
public class RequestValidatorEdgeCaseTest
{
    [Fact]
    public void TestNullSignature()
    {
        var validator = new RequestValidator("key");
        var payload = new Dictionary<string, string> { { "a", "b" } };
        validator.Validate("http://example.com", payload, null).Should().BeFalse();
    }

    [Fact]
    public void TestHttpsDefaultPort()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        // HTTPS URL without explicit port should normalize to :443
        validator.Validate("https://example.com/callbacks", payload, "57ba6c3c14ea4bfa9bebd079869cafb27dcba1b6").Should().BeTrue();
    }
}

// --- OrderItemConverter null write path ---
public class OrderItemConverterEdgeTest
{
    [Fact]
    public void TestSerializeNullItems()
    {
        var order = new Order
        {
            Items = null
        };

        var settings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };
        var json = JsonConvert.SerializeObject(order, settings);
        // Should not throw, items serialized as null
        json.Should().NotBeNull();
    }
}

// --- TrunkConfigurationConverter null write path ---
public class TrunkConfigurationConverterEdgeTest
{
    [Fact]
    public void TestSerializeNullConfiguration()
    {
        var trunk = new VoiceInTrunk
        {
            Name = "test",
            Configuration = null
        };

        var settings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };
        var json = JsonConvert.SerializeObject(trunk, settings);
        json.Should().NotBeNull();
    }

    [Fact]
    public void TestDeserializeNullConfiguration()
    {
        // A trunk with null configuration attribute
        var json = """
        {
            "data": {
                "id": "abc",
                "type": "voice_in_trunks",
                "attributes": {
                    "name": "test",
                    "configuration": null
                }
            }
        }
        """;

        var settings = new JsonApiSerializerSettings();
        var trunk = JsonConvert.DeserializeObject<VoiceInTrunk>(json, settings);
        trunk.Should().NotBeNull();
        trunk!.Configuration.Should().BeNull();
    }
}

// --- DidwwEnvironment edge case ---
public class DidwwEnvironmentTest
{
    [Fact]
    public void TestProductionEnvironment()
    {
        var creds = new DidwwCredentials("key", DidwwEnvironment.Production);
        creds.GetBaseUrl().Should().Contain("api.didww.com");
    }

    [Fact]
    public void TestSandboxEnvironment()
    {
        var creds = new DidwwCredentials("key", DidwwEnvironment.Sandbox);
        creds.GetBaseUrl().Should().Contain("sandbox");
    }
}

// --- Builder SetInnerHandler coverage ---
public class DidwwClientBuilderTest
{
    [Fact]
    public void TestSetInnerHandler()
    {
        var handler = new HttpClientHandler();
        var credentials = new DidwwCredentials("test-key", DidwwEnvironment.Sandbox);
        var client = DidwwClient.NewBuilder()
            .SetCredentials(credentials)
            .SetInnerHandler(handler)
            .Build();

        client.Should().NotBeNull();
    }

    [Fact]
    public void TestSetTimeout()
    {
        var credentials = new DidwwCredentials("test-key", DidwwEnvironment.Sandbox);
        var client = DidwwClient.NewBuilder()
            .SetCredentials(credentials)
            .SetTimeout(TimeSpan.FromSeconds(30))
            .Build();

        client.Should().NotBeNull();
    }
}

// --- Export create test ---
public class ExportCreateTest : BaseTest
{
    [Fact]
    public async Task TestCreateExport()
    {
        StubPost("exports", "exports/create.json");

        var export = new Export
        {
            ExportType = ExportType.CdrIn,
            Filters = new Dictionary<string, object>
            {
                { "year", 2019 },
                { "month", 1 }
            }
        };

        var response = await Client.Exports().CreateAsync(export);
        response.Data.Id.Should().Be("da15f006-5da4-45ca-b0df-735baeadf423");
        response.Data.Status.Should().NotBeNull();
    }

    [Fact]
    public async Task TestShowExport()
    {
        StubGet("exports/da15f006-5da4-45ca-b0df-735baeadf423", "exports/show.json");

        var response = await Client.Exports()
            .FindAsync("da15f006-5da4-45ca-b0df-735baeadf423");
        response.Data.Url.Should().NotBeNull();
        response.Data.ExportType.Should().Be(ExportType.CdrIn);
    }
}
