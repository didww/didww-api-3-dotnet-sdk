using System.Net;
using Didww.Api3.Auth;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Didww.Api3.Tests;

public class DidwwClientTest
{
    [Fact]
    public void TestProductionBaseUrl()
    {
        var credentials = new DidwwCredentials("test-key", DidwwEnvironment.Production);
        var client = DidwwClient.NewBuilder()
            .SetCredentials(credentials)
            .Build();

        client.BaseUrl.Should().Be("https://api.didww.com/v3");
    }

    [Fact]
    public void TestSandboxBaseUrl()
    {
        var credentials = new DidwwCredentials("test-key", DidwwEnvironment.Sandbox);
        var client = DidwwClient.NewBuilder()
            .SetCredentials(credentials)
            .Build();

        client.BaseUrl.Should().Be("https://sandbox-api.didww.com/v3");
    }

    [Fact]
    public void TestCustomBaseUrl()
    {
        var credentials = new DidwwCredentials("test-key", DidwwEnvironment.Sandbox);
        var client = DidwwClient.NewBuilder()
            .SetCredentials(credentials)
            .SetBaseUrl("http://localhost:8080/v3")
            .Build();

        client.BaseUrl.Should().Be("http://localhost:8080/v3");
    }

    [Fact]
    public void TestBuilderRequiresCredentials()
    {
        var act = () => DidwwClient.NewBuilder().Build();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TestSdkUserAgentContainsVersion()
    {
        DidwwClient.SdkUserAgent.Should().StartWith("didww-dotnet-sdk/");
        DidwwClient.SdkUserAgent.Should().MatchRegex(@"^didww-dotnet-sdk/\d+\.\d+\.\d+$");
    }

    [Fact]
    public void TestDefaultApiVersion()
    {
        DidwwClient.DefaultApiVersion.Should().Be("2026-10-22");

        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .Build();

        client.ApiVersion.Should().Be(DidwwClient.DefaultApiVersion);
    }

    [Fact]
    public void TestSetApiVersion()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetApiVersion("2099-01-01")
            .Build();

        client.ApiVersion.Should().Be("2099-01-01");
    }

    [Fact]
    public void TestSetApiVersionRejectsInvalidFormat()
    {
        // Unicode digits and a trailing newline must not pass for YYYY-MM-DD.
        foreach (var version in new[] { "", "latest", "2026-10-22T00:00:00Z", "26-10-22", "2026-10-22\n", "\u0662026-10-22" })
        {
            var act = () => DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
                .SetApiVersion(version)
                .Build();

            act.Should().Throw<ArgumentException>().WithMessage("*YYYY-MM-DD*", version);
        }
    }

    [Fact]
    public void TestSetApiVersionRejectsOlderVersion()
    {
        var act = () => DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetApiVersion("2026-04-16")
            .Build();

        act.Should().Throw<ArgumentException>()
            .WithMessage("API version 2026-04-16 is not supported: this SDK requires 2026-10-22 or later; " +
                         "use Didww.Api3 3.x (branch release-3) for 2026-04-16");
    }

    [Theory]
    [InlineData(DidwwScope.Read)]
    [InlineData(DidwwScope.Full)]
    public void TestSetScope(DidwwScope scope)
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetScope(scope)
            .Build();

        client.Scope.Should().Be(scope);
    }

    [Fact]
    public void TestSetScopeRejectsUndefinedValue()
    {
        var act = () => DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetScope((DidwwScope)7)
            .Build();

        act.Should().Throw<ArgumentException>().WithMessage("Scope must be*");
    }

    [Fact]
    public void TestApiKeyValidation()
    {
        foreach (var apiKey in new[] { "", "test-api-key\n", "test\rkey", "test\u0000key", "test\u007fkey", "test-api-key\u00a0", "test\u200bkey" })
        {
            var act = () => DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(apiKey, DidwwEnvironment.Sandbox))
                .Build();

            act.Should().Throw<ArgumentException>().WithMessage("API key*");
        }
    }

    [Fact]
    public void TestApiKeyWithTabIsAllowed()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test\tkey", DidwwEnvironment.Sandbox))
            .Build();

        client.Should().NotBeNull();
    }

    [Fact]
    public void TestUnknownEnvironmentRejected()
    {
        var act = () => DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", (DidwwEnvironment)7))
            .Build();

        act.Should().Throw<ArgumentException>()
            .WithMessage("Environment must be DidwwEnvironment.Sandbox or DidwwEnvironment.Production*");
    }

    [Fact]
    public void TestSandboxEndpoints()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .Build();

        client.AuthUrl.Should().Be("https://auth-sandbox.didww.com");
        client.VoiceInPricesUrl.Should().Be("https://api-vin-sandbox.didww.com/v3");
        client.VoiceOutPricesUrl.Should().Be("https://api-vout-sandbox.didww.com/v3");
        client.SmsPricesUrl.Should().Be("https://api-sms-sandbox.didww.com/v3");
    }

    [Fact]
    public void TestProductionEndpoints()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Production))
            .Build();

        client.AuthUrl.Should().Be("https://auth.didww.com");
        client.VoiceInPricesUrl.Should().Be("https://api-vin.didww.com/v3");
        client.VoiceOutPricesUrl.Should().Be("https://api-vout.didww.com/v3");
        client.SmsPricesUrl.Should().Be("https://api-sms.didww.com/v3");
        DidwwEnvironment.Production.GetAuthUrl().Should().Be("https://auth.didww.com");
        DidwwEnvironment.Sandbox.GetSmsPricesUrl().Should().Be("https://api-sms-sandbox.didww.com/v3");
    }

    [Fact]
    public void TestEndpointOverrides()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Production))
            .SetBaseUrl("http://localhost:3000")
            .SetAuthUrl("http://localhost:3001")
            .SetVoiceInPricesUrl("http://localhost:3002/v3/")
            .SetVoiceOutPricesUrl("http://localhost:3003")
            .SetSmsPricesUrl("http://localhost:3004/v3")
            .Build();

        client.BaseUrl.Should().Be("http://localhost:3000");
        client.AuthUrl.Should().Be("http://localhost:3001");
        client.VoiceInPricesUrl.Should().Be("http://localhost:3002/v3/");
        client.VoiceOutPricesUrl.Should().Be("http://localhost:3003");
        client.SmsPricesUrl.Should().Be("http://localhost:3004/v3");
    }

    [Fact]
    public void TestCustomBaseUrlKeepsTheEnvironmentTokenService()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetBaseUrl("http://localhost:3000/v3")
            .Build();

        client.AuthUrl.Should().Be("https://auth-sandbox.didww.com");
    }

    [Theory]
    [InlineData("Token service", "auth.didww.com")]
    [InlineData("Voice IN prices", "api-prices.example.com")]
    [InlineData("Voice OUT prices", "ftp://api-prices.example.com")]
    [InlineData("SMS prices", "/v3")]
    [InlineData("API base", "localhost:3000/v3")]
    public void TestUrlValidation(string name, string url)
    {
        var builder = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox));
        switch (name)
        {
            case "Token service":
                builder.SetAuthUrl(url);
                break;
            case "Voice IN prices":
                builder.SetVoiceInPricesUrl(url);
                break;
            case "Voice OUT prices":
                builder.SetVoiceOutPricesUrl(url);
                break;
            case "SMS prices":
                builder.SetSmsPricesUrl(url);
                break;
            default:
                builder.SetBaseUrl(url);
                break;
        }

        var act = () => builder.Build();

        act.Should().Throw<ArgumentException>().WithMessage($"{name} URL must be an http or https URL*");
    }

    [Fact]
    public void TestJoinV3()
    {
        DidwwHttp.JoinV3("http://h", "countries").Should().Be("http://h/v3/countries");
        DidwwHttp.JoinV3("http://h/v3", "countries").Should().Be("http://h/v3/countries");
        DidwwHttp.JoinV3("http://h/v3/", "countries").Should().Be("http://h/v3/countries");
        DidwwHttp.JoinV3("http://h/", "/sms_in/prices.csv.gz").Should().Be("http://h/v3/sms_in/prices.csv.gz");
    }

    [Fact]
    public void TestCredentialsToStringHidesApiKey()
    {
        var credentials = new DidwwCredentials("secret-api-key", DidwwEnvironment.Sandbox);

        credentials.ToString().Should().NotContain("secret-api-key");
        credentials.ToString().Should().Contain("[FILTERED]");
    }

    [Fact]
    public void TestDefaultTimeout()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .Build();

        client.HttpClient.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void TestDefaultInnerHandler()
    {
        var handler = DidwwHttp.DefaultHandler.Value;

        handler.AllowAutoRedirect.Should().BeFalse();
        handler.UseCookies.Should().BeFalse();
        handler.AutomaticDecompression.Should().Be(DecompressionMethods.None);
        handler.ConnectTimeout.Should().Be(TimeSpan.FromSeconds(10));
        handler.PooledConnectionLifetime.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task TestRequestSendsBearerTokenWithoutApiKeyOrVersionHeader()
    {
        using var wireMock = WireMockServer.Start();
        StubToken(wireMock);
        var client = NewClient(wireMock);
        StubCountries(wireMock);

        await client.Countries().ListAsync();

        var request = wireMock.LogEntries.Single(e => e.RequestMessage.Path == "/v3/countries").RequestMessage;
        request.Headers!["Authorization"].Should().ContainSingle().Which.Should().Be("Bearer test-access-token");
        request.Headers.Should().NotContainKey("Api-Key");
        request.Headers.Should().NotContainKey("X-DIDWW-API-Version");

        var mint = wireMock.LogEntries.Single(e => e.RequestMessage.Path == "/token").RequestMessage;
        mint.Headers!["Api-Key"].Should().ContainSingle().Which.Should().Be("test-key");
        JToken.DeepEquals(JToken.Parse(mint.Body!), JToken.Parse("""{"aud":["api.didww.com"],"api_version":"2026-10-22"}"""))
            .Should().BeTrue();
    }

    [Fact]
    public async Task TestRequestIncludesUserAgentHeader()
    {
        using var wireMock = WireMockServer.Start();
        StubToken(wireMock);
        var client = NewClient(wireMock);
        StubCountries(wireMock);

        await client.Countries().ListAsync();

        var request = wireMock.LogEntries.Single(e => e.RequestMessage.Path == "/v3/countries").RequestMessage;
        request.Headers!["User-Agent"].First().Should().Be(DidwwClient.SdkUserAgent);
    }

    [Fact]
    public async Task TestDefaultHandlerSendsNoCookies()
    {
        using var wireMock = WireMockServer.Start();
        StubToken(wireMock);
        wireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithHeader("Set-Cookie", "session=abc; Path=/")
                .WithBody("{\"data\":[]}")
        );
        var client = NewClient(wireMock);

        await client.Countries().ListAsync();
        await client.Countries().ListAsync();

        var requests = wireMock.LogEntries.Where(e => e.RequestMessage.Path == "/v3/countries").ToList();
        requests.Should().HaveCount(2);
        requests[1].RequestMessage.Headers.Should().NotContainKey("Cookie");
    }

    [Fact]
    public async Task TestTokenMintDoesNotFollowRedirects()
    {
        using var wireMock = WireMockServer.Start();
        wireMock.Given(
            Request.Create().WithPath("/token").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(307).WithHeader("Location", wireMock.Url + "/elsewhere/token")
        );
        wireMock.Given(
            Request.Create().WithPath("/elsewhere/token")
        ).RespondWith(
            Response.Create().WithStatusCode(200)
        );
        var client = NewClient(wireMock);

        var act = () => client.Countries().ListAsync();

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.UnexpectedStatus);
        e.StatusCode.Should().Be(307);
        wireMock.LogEntries.Count(x => x.RequestMessage.Path == "/token").Should().Be(1);
        wireMock.LogEntries.Should().NotContain(x => x.RequestMessage.Path == "/elsewhere/token");
    }

    [Fact]
    public async Task TestTokenMintRefusesRedirectFollowedByInnerHandler()
    {
        using var wireMock = WireMockServer.Start();
        wireMock.Given(
            Request.Create().WithPath("/token").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(307).WithHeader("Location", wireMock.Url + "/elsewhere/token")
        );
        wireMock.Given(
            Request.Create().WithPath("/elsewhere/token").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(request => FakeAuthServer.TokenJson(request.Body, "test-access-token"))
        );
        using var followingHandler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetBaseUrl(wireMock.Url + "/v3")
            .SetAuthUrl(wireMock.Url!)
            .SetLogger(null)
            .SetInnerHandler(followingHandler)
            .Build();

        var act = () => client.Countries().ListAsync();

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.UnexpectedStatus);
        e.Message.Should().Contain("redirected");
        wireMock.LogEntries.Should().Contain(x => x.RequestMessage.Path == "/elsewhere/token",
            "the inner handler followed the redirect");
        wireMock.LogEntries.Should().NotContain(x => x.RequestMessage.Path.StartsWith("/v3", StringComparison.Ordinal),
            "no API request is sent");
    }

    private static DidwwClient NewClient(WireMockServer wireMock) => DidwwClient.NewBuilder()
        .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
        .SetBaseUrl(wireMock.Url + "/v3")
        .SetAuthUrl(wireMock.Url!)
        .SetLogger(null)
        .Build();

    private static void StubToken(WireMockServer wireMock)
    {
        wireMock.Given(
            Request.Create().WithPath("/token").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(request => FakeAuthServer.TokenJson(request.Body, "test-access-token"))
        );
    }

    private static void StubCountries(WireMockServer wireMock)
    {
        wireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("{\"data\":[]}")
        );
    }

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
