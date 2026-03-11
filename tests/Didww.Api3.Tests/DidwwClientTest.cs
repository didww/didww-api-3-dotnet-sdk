using FluentAssertions;
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
    public async Task TestRequestIncludesUserAgentHeader()
    {
        using var wireMock = WireMockServer.Start();
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-key", DidwwEnvironment.Sandbox))
            .SetBaseUrl(wireMock.Url + "/v3")
            .Build();

        wireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("{\"data\":[]}")
        );

        await client.Countries().ListAsync();

        var request = wireMock.LogEntries.First().RequestMessage;
        request.Headers!["User-Agent"].First().Should().Be(DidwwClient.SdkUserAgent);
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
