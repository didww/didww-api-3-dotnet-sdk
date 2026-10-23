using System.Net;
using WireMock;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Didww.Api3.Tests;

public abstract class BaseTest : IDisposable
{
    /// <summary>The access token granted by the stubbed token service.</summary>
    protected const string TestAccessToken = "test-access-token";

    protected WireMockServer WireMock { get; }
    protected DidwwClient Client { get; }

    protected BaseTest()
    {
        WireMock = WireMockServer.Start();
        StubToken();

        Client = NewClientBuilder().Build();
    }

    /// <summary>
    /// A builder whose JSON:API, token service and prices URLs all point at the
    /// WireMock server, with logging disabled.
    /// </summary>
    protected DidwwClient.Builder NewClientBuilder() => DidwwClient.NewBuilder()
        .SetCredentials(new DidwwCredentials("test-api-key", DidwwEnvironment.Sandbox))
        .SetBaseUrl(WireMock.Url + "/v3")
        .SetAuthUrl(WireMock.Url!)
        .SetVoiceInPricesUrl(WireMock.Url!)
        .SetVoiceOutPricesUrl(WireMock.Url!)
        .SetSmsPricesUrl(WireMock.Url!)
        .SetLogger(null)
        .SetTimeout(TimeSpan.FromSeconds(5));

    /// <summary>
    /// Serves POST /token with <paramref name="accessToken"/>, echoing the
    /// requested audience and API version, scope "full" and expires_in 900.
    /// </summary>
    protected void StubToken(string accessToken = TestAccessToken)
    {
        WireMock.Given(
            Request.Create().WithPath("/token").UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(request => FakeAuthServer.TokenJson(request.Body, accessToken))
        );
    }

    /// <summary>The number of token requests received.</summary>
    protected int MintCount => WireMock.LogEntries.Count(e => e.RequestMessage.Path == "/token");

    /// <summary>The requests received on <paramref name="path"/>, in order.</summary>
    protected List<IRequestMessage> RequestsTo(string path) => WireMock.LogEntries
        .Where(e => e.RequestMessage.Path == path)
        .Select(e => e.RequestMessage)
        .ToList();

    protected string LoadFixture(string path)
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixtures", path);
        return File.ReadAllText(fixturePath);
    }

    protected void StubGet(string urlPath, string fixturePath, int status = 200)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(status)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture(fixturePath))
        );
    }

    protected void StubPost(string urlPath, string responseFixturePath, int status = 201)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingPost()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(status)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture(responseFixturePath))
        );
    }

    protected void StubPost(string urlPath, string requestFixturePath, string responseFixturePath, int status = 201)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingPost()
                .WithBody(new JsonMatcher(LoadFixture(requestFixturePath)))
        ).RespondWith(
            Response.Create()
                .WithStatusCode(status)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture(responseFixturePath))
        );
    }

    protected void StubPatch(string urlPath, string responseFixturePath, int status = 200)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingPatch()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(status)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture(responseFixturePath))
        );
    }

    protected void StubPatch(string urlPath, string requestFixturePath, string responseFixturePath, int status = 200)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingPatch()
                .WithBody(new JsonMatcher(LoadFixture(requestFixturePath)))
        ).RespondWith(
            Response.Create()
                .WithStatusCode(status)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture(responseFixturePath))
        );
    }

    protected void StubDelete(string urlPath, int status = 204)
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/" + urlPath).UsingDelete()
        ).RespondWith(
            Response.Create().WithStatusCode(status)
        );
    }

    public void Dispose()
    {
        WireMock?.Dispose();
        GC.SuppressFinalize(this);
    }
}
