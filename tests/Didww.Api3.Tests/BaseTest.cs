using System.Net;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Didww.Api3.Tests;

public abstract class BaseTest : IDisposable
{
    protected WireMockServer WireMock { get; }
    protected DidwwClient Client { get; }

    protected BaseTest()
    {
        WireMock = WireMockServer.Start();

        var baseUrl = WireMock.Url + "/v3";
        Client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-api-key", DidwwEnvironment.Sandbox))
            .SetBaseUrl(baseUrl)
            .SetTimeout(TimeSpan.FromSeconds(5))
            .Build();
    }

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
