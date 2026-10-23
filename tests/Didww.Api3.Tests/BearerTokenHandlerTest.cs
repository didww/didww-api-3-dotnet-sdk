using System.Net;
using Didww.Api3.Auth;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

public class BearerTokenHandlerTest : IDisposable
{
    private const string UnauthorizedBody =
        """{"errors":[{"title":"Unauthorized","detail":"Authorization failed","code":"401","status":"401"}]}""";

    private const string ApiMintBody = """{"aud":["api.didww.com"],"api_version":"2026-10-22"}""";

    private readonly FakeAuthServer _server = new();

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    private static void AssertJsonEquals(string actual, string expected) =>
        JToken.DeepEquals(JToken.Parse(actual), JToken.Parse(expected)).Should()
            .BeTrue($"expected {expected}, got {actual}");

    private static HttpResponseMessage Unauthorized() =>
        FakeAuthServer.Json(401, UnauthorizedBody, "application/vnd.api+json");

    [Fact]
    public async Task TestClientSendsBearerTokenAndNoApiKey()
    {
        _server.Api = (_, _) => Task.FromResult(FakeAuthServer.Fixture(200, "balance/index.json"));
        var client = _server.NewClient();

        await client.Balance().FindAsync();

        var request = _server.ApiRequests.Should().ContainSingle().Subject;
        request.Authorization.Should().Be("Bearer token-1");
        request.Headers["Accept"].Should().Be("application/vnd.api+json");
        request.Headers["User-Agent"].Should().Be(DidwwClient.SdkUserAgent);
        request.Headers.Should().NotContainKey("Api-Key");
        request.Headers.Should().NotContainKey("X-DIDWW-API-Version");
        AssertJsonEquals(_server.MintBody(0), ApiMintBody);
    }

    [Fact]
    public async Task TestClientRetriesOnceWithNewTokenAfter401()
    {
        _server.Api = (r, _) => Task.FromResult(r.Authorization == "Bearer token-1"
            ? Unauthorized()
            : FakeAuthServer.Fixture(200, "countries/index.json"));
        var client = _server.NewClient();

        var countries = await client.Countries().ListAsync();

        countries.Data.Should().NotBeEmpty();
        _server.Mints.Should().Be(2);
        AssertJsonEquals(_server.MintBody(0), ApiMintBody);
        AssertJsonEquals(_server.MintBody(1), ApiMintBody);
        _server.ApiRequests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
    }

    [Fact]
    public async Task TestClientReplaysPatchBodyAfter401()
    {
        _server.Api = (r, _) => Task.FromResult(r.Authorization == "Bearer token-1"
            ? Unauthorized()
            : FakeAuthServer.Fixture(200, "exports/update_external_reference_id.json"));
        var client = _server.NewClient();
        var export = Export.Build("21e02b15-806d-44b3-b67f-434ea6c44f61");
        export.ExternalReferenceId = "renamed-ref-99";

        var response = await client.Exports().UpdateAsync(export);

        response.Data.Id.Should().Be("21e02b15-806d-44b3-b67f-434ea6c44f61");
        var requests = _server.ApiRequests;
        requests.Should().HaveCount(2);
        requests.Should().OnlyContain(r => r.Method == "PATCH");
        AssertJsonEquals(requests[0].Body, FakeAuthServer.LoadFixture("exports/update_external_reference_id_request.json"));
        requests[1].Body.Should().Be(requests[0].Body);
        requests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
    }

    [Fact]
    public async Task TestClientSecond401DropsReplacementToken()
    {
        _server.Api = (r, _) => Task.FromResult(r.Authorization != "Bearer token-3"
            ? Unauthorized()
            : FakeAuthServer.Fixture(200, "countries/index.json"));
        var client = _server.NewClient();

        var act = () => client.Countries().ListAsync();
        (await act.Should().ThrowAsync<DidwwApiException>()).Which.HttpStatus.Should().Be(401);
        _server.ApiRequests.Should().HaveCount(2);
        _server.Mints.Should().Be(2);

        await client.Countries().ListAsync();

        _server.ApiRequests.Select(r => r.Authorization)
            .Should().Equal("Bearer token-1", "Bearer token-2", "Bearer token-3");
        _server.Mints.Should().Be(3);
    }

    [Fact]
    public async Task TestClientForbiddenNotRetried()
    {
        _server.Api = (_, _) =>
        {
            var response = FakeAuthServer.Json(403,
                """{"errors":[{"title":"Insufficient scope","code":"403","status":"403"}]}""",
                "application/vnd.api+json");
            response.Headers.TryAddWithoutValidation("WWW-Authenticate", "Bearer error=\"insufficient_scope\"");
            return Task.FromResult(response);
        };
        var client = _server.NewClient(b => b.SetScope(DidwwScope.Read));

        var act = () => client.VoiceInTrunks().DeleteAsync("41b94706-325e-4704-a433-d65105758836");

        var e = (await act.Should().ThrowAsync<DidwwApiException>()).Which;
        e.HttpStatus.Should().Be(403);
        e.Errors[0].Title.Should().Be("Insufficient scope");
        _server.ApiRequests.Should().HaveCount(1);
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestClientKeepsTokenMintedByAnotherCaller()
    {
        DidwwClient? client = null;
        _server.Api = async (r, _) =>
        {
            if (r.Authorization != "Bearer token-1")
                return FakeAuthServer.Fixture(200, "countries/index.json");
            // Another caller replaces the token before this answer arrives.
            await client!.Tokens.InvalidateAsync(TokenAudience.Api, "token-1", CancellationToken.None);
            await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);
            return Unauthorized();
        };
        client = _server.NewClient();

        await client.Countries().ListAsync();

        _server.Mints.Should().Be(2);
        _server.ApiRequests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
    }

    [Fact]
    public async Task TestClientRaisesAuthExceptions()
    {
        _server.Mint = (_, _, _) => Task.FromResult(
            FakeAuthServer.Json(401, FakeAuthServer.LoadFixture("token/invalid_client.json")));
        var client = _server.NewClient();

        var act = () => client.Countries().ListAsync();

        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.InvalidClient);
        _server.ApiRequests.Should().BeEmpty();
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestClientPropagatesTransportErrors()
    {
        _server.Api = (_, _) => throw new HttpRequestException("connection reset");
        var client = _server.NewClient();

        var act = () => client.Countries().ListAsync();

        (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().Be("connection reset");
    }

    [Fact]
    public async Task TestPublicKeysSentWithoutToken()
    {
        _server.Api = (_, _) => Task.FromResult(FakeAuthServer.Fixture(200, "public_keys/index.json"));
        var client = _server.NewClient();

        var keys = await client.PublicKeys().ListAsync();

        keys.Data.Should().HaveCount(2);
        var request = _server.ApiRequests.Should().ContainSingle().Subject;
        request.Headers.Should().NotContainKey("Authorization");
        request.Headers.Should().NotContainKey("Api-Key");
        request.Headers.Should().NotContainKey("X-DIDWW-API-Version");
        request.Headers["User-Agent"].Should().Be(DidwwClient.SdkUserAgent);
        _server.Mints.Should().Be(0);
    }

    [Theory]
    [InlineData("https://api.example.test/v3/public_keys", true)]
    [InlineData("https://api.example.test/v3/public_keys/dcf2bfcb", true)]
    [InlineData("https://proxy.example.test/didww/v3/public_keys", true)]
    [InlineData("https://api.example.test/v3/dids?filter[public_keys]=1", false)]
    [InlineData("https://api.example.test/v3/public_keys_archive", false)]
    [InlineData("https://api.example.test/v3/exports/public_keys", false)]
    [InlineData("https://api.example.test/public_keys", false)]
    public void TestIsPublicEndpoint(string url, bool expected)
    {
        BearerTokenHandler.IsPublicEndpoint(new Uri(url)).Should().Be(expected);
    }

    [Fact]
    public async Task TestSendStripsIfNoneMatchAndUsesRequestAudience()
    {
        _server.Api = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified));
        var client = _server.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api-vin.example.test/v3/prices.csv.gz");
        request.Options.Set(DidwwRequestOptions.Audience, TokenAudience.VoiceIn);
        request.Headers.TryAddWithoutValidation("If-None-Match", "\"abc\"");
        request.Headers.TryAddWithoutValidation("If-Modified-Since", "Wed, 14 Oct 2026 06:00:00 GMT");

        using var response = await client.HttpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
        var sent = _server.ApiRequests.Should().ContainSingle().Subject;
        sent.Headers.Should().NotContainKey("If-None-Match");
        sent.Headers.Should().NotContainKey("Accept");
        sent.Headers["If-Modified-Since"].Should().Be("Wed, 14 Oct 2026 06:00:00 GMT");
        sent.Authorization.Should().Be("Bearer token-1");
        AssertJsonEquals(_server.MintBody(0), """{"aud":["api-vin.didww.com"],"api_version":"2026-10-22"}""");
    }

    [Fact]
    public async Task TestNonReplayableContentNotRetriedAfter401()
    {
        _server.Api = (_, _) => Task.FromResult(Unauthorized());
        var client = _server.NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, FakeAuthServer.BaseUrl + "/encrypted_files")
        {
            Content = new StreamContent(new NonSeekableStream(new byte[] { 1, 2, 3 }))
        };

        using var response = await client.HttpClient.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _server.ApiRequests.Should().HaveCount(1);
        await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);
        _server.Mints.Should().Be(2, "the refused token was evicted");
    }

    [Fact]
    public async Task TestRedirectedRequestNotRetriedAfter401()
    {
        var redirecting = new RedirectingHandler(_server, new Uri("https://other.example.test/x"));
        var client = _server.NewClient(b => b.SetInnerHandler(redirecting));
        using var destination = new MemoryStream();

        var act = () => client.DownloadExportAsync(FakeAuthServer.BaseUrl + "/exports/x.csv.gz", destination);

        (await act.Should().ThrowAsync<DidwwClientException>()).Which.Message.Should().Contain("redirected");
        redirecting.Sends.Should().Be(1, "a fresh token is not sent to the redirect target");
        _server.Mints.Should().Be(1);
        (await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None)).AccessToken
            .Should().Be("token-1", "the token is not evicted");
        destination.Length.Should().Be(0);
    }

    [Fact]
    public void TestIsReplayable()
    {
        BearerTokenHandler.IsReplayable(null).Should().BeTrue();
        BearerTokenHandler.IsReplayable(new StringContent("x")).Should().BeTrue();
        BearerTokenHandler.IsReplayable(new ByteArrayContent(new byte[] { 1 })).Should().BeTrue();
        BearerTokenHandler.IsReplayable(new FormUrlEncodedContent(new Dictionary<string, string> { ["a"] = "b" }))
            .Should().BeTrue();
        BearerTokenHandler.IsReplayable(new MultipartFormDataContent
        {
            { new StringContent("x"), "a" },
            { new ByteArrayContent(new byte[] { 1 }), "b", "b.bin" }
        }).Should().BeTrue();
        BearerTokenHandler.IsReplayable(new StreamContent(new MemoryStream())).Should().BeFalse();
        BearerTokenHandler.IsReplayable(new MultipartFormDataContent
        {
            { new StreamContent(new MemoryStream()), "a", "a.bin" }
        }).Should().BeFalse();
    }

    // Answers every API request as a followed redirect would: the request now
    // points at target, without credentials, and the target refuses it with 401.
    private sealed class RedirectingHandler : DelegatingHandler
    {
        private readonly Uri _target;
        private int _sends;

        public RedirectingHandler(HttpMessageHandler inner, Uri target) : base(inner)
        {
            _target = target;
        }

        public int Sends => Volatile.Read(ref _sends);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.GetLeftPart(UriPartial.Authority) == FakeAuthServer.AuthUrl)
                return base.SendAsync(request, cancellationToken);

            Interlocked.Increment(ref _sends);
            request.RequestUri = _target;
            request.Headers.Authorization = null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { RequestMessage = request });
        }
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public NonSeekableStream(byte[] buffer) : base(buffer)
        {
        }

        public override bool CanSeek => false;
    }
}
