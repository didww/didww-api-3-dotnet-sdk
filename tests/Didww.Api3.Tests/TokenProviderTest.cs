using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using Didww.Api3.Auth;
using Didww.Api3.Exception;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

public class TokenProviderTest : IDisposable
{
    private const string Api = TokenAudience.Api;
    private const string VoiceIn = TokenAudience.VoiceIn;

    private readonly FakeAuthServer _server = new();

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    private static HttpResponseMessage TokenError(int status, string fixture) =>
        FakeAuthServer.Json(status, FakeAuthServer.LoadFixture(fixture));

    private static void AssertJsonEquals(string actual, string expected) =>
        JToken.DeepEquals(JToken.Parse(actual), JToken.Parse(expected)).Should()
            .BeTrue($"expected {expected}, got {actual}");

    [Fact]
    public async Task TestTokenMintRequest()
    {
        var client = _server.NewClient();

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-1");
        token.TokenType.Should().Be("Bearer");
        token.ExpiresIn.Should().Be(900);
        token.Audience.Should().Equal(Api);
        token.Scope.Should().Be("full");
        token.ApiVersion.Should().Be(DidwwClient.DefaultApiVersion);
        token.MintedAt.Should().NotBe(default);

        _server.Mints.Should().Be(1);
        AssertJsonEquals(_server.MintBody(0), FakeAuthServer.LoadFixture("token/create_request.json"));
        var headers = _server.MintHeaders(0);
        headers["Api-Key"].Should().Be(client.Credentials.ApiKey);
        headers["Content-Type"].Should().StartWith("application/json");
        headers["Accept"].Should().Be("application/json");
        headers["User-Agent"].Should().Be(DidwwClient.SdkUserAgent);
        headers.Should().NotContainKey("Authorization");
    }

    [Fact]
    public async Task TestTokenMintRequestWithScope()
    {
        var client = _server.NewClient(b => b.SetScope(DidwwScope.Read));

        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        AssertJsonEquals(_server.MintBody(0), FakeAuthServer.LoadFixture("token/create_request_read_scope.json"));
    }

    [Fact]
    public async Task TestTokenMintRequestWithApiVersion()
    {
        var client = _server.NewClient(b => b.SetApiVersion("2099-01-01"));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.ApiVersion.Should().Be("2099-01-01");
        AssertJsonEquals(_server.MintBody(0), """{"aud":["api.didww.com"],"api_version":"2099-01-01"}""");
    }

    [Fact]
    public async Task TestTokenReused()
    {
        var client = _server.NewClient();

        var first = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        var second = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        second.AccessToken.Should().Be(first.AccessToken);
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenMintedPerAudience()
    {
        var client = _server.NewClient();

        var api = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        var vin = await client.Tokens.GetTokenAsync(VoiceIn, CancellationToken.None);

        vin.AccessToken.Should().NotBe(api.AccessToken);
        vin.Audience.Should().Equal(VoiceIn);
        _server.Mints.Should().Be(2);
        AssertJsonEquals(_server.MintBody(0), """{"aud":["api.didww.com"],"api_version":"2026-10-22"}""");
        AssertJsonEquals(_server.MintBody(1), """{"aud":["api-vin.didww.com"],"api_version":"2026-10-22"}""");
    }

    [Fact]
    public async Task TestTokenSharedThroughStore()
    {
        var store = new MemoryTokenStore();
        var apiKey = "key-" + Guid.NewGuid().ToString("N");
        DidwwClient NewClient() => DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials(apiKey, DidwwEnvironment.Sandbox))
            .SetBaseUrl(FakeAuthServer.BaseUrl)
            .SetAuthUrl(FakeAuthServer.AuthUrl)
            .SetLogger(null)
            .SetInnerHandler(_server)
            .SetTokenStore(store)
            .Build();

        await NewClient().Tokens.GetTokenAsync(Api, CancellationToken.None);
        var token = await NewClient().Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenCachedForGrantedLifetime()
    {
        _server.Mint = (_, _, _) => Task.FromResult(FakeAuthServer.Json(200,
            """{"access_token":"t","token_type":"Bearer","expires_in":300,"aud":["api.didww.com"],"scope":"full","api_version":"2026-10-22"}"""));
        var keys = new List<string>();
        var ttls = new List<TimeSpan>();
        var store = new DelegatingTokenStore();
        store.OnSet = (key, token, ttl, ct) =>
        {
            keys.Add(key);
            ttls.Add(ttl);
            return store.Inner.SetAsync(key, token, ttl, ct);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store));

        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        keys.Should().ContainSingle().Which.Should().StartWith("didww:token:v1:");
        ttls.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void TestTokenCacheKey()
    {
        static TokenProvider Provider(string apiKey = "key-1", Action<DidwwClient.Builder>? configure = null)
        {
            var builder = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(apiKey, DidwwEnvironment.Production))
                .SetAuthUrl("https://auth.example.com");
            configure?.Invoke(builder);
            return builder.Build().Tokens;
        }

        var key = Provider().CacheKey(Api);

        key.Should().Be("didww:token:v1:2e1c5435f0e67f91d34497fabf69840a945923a47982fad172c353b3f26be539");
        key.Should().MatchRegex("^didww:token:v1:[0-9a-f]{64}$");
        key.Should().NotContain("key-1");
        Provider(configure: b => b.SetAuthUrl("https://auth.example.com/")).CacheKey(Api).Should().Be(key);
        Provider(configure: b => b.SetScope(DidwwScope.Read)).CacheKey(Api)
            .Should().Be("didww:token:v1:7b0667b745747b7de833d43c46a3cd1e3e630384fe8595b7789f5074cb6617c0");
        Provider().CacheKey(TokenAudience.Sms).Should().NotBe(key);
        Provider(configure: b => b.SetApiVersion("2099-01-01")).CacheKey(Api).Should().NotBe(key);
        Provider(configure: b => b.SetAuthUrl("https://auth-sandbox.example.com")).CacheKey(Api).Should().NotBe(key);
        Provider("key-2").CacheKey(Api).Should().NotBe(key);
    }

    [Fact]
    public async Task TestTokenSingleFlight()
    {
        _server.Mint = async (n, body, ct) =>
        {
            await Task.Delay(50, ct);
            return FakeAuthServer.TokenResponse(body, $"token-{n}");
        };
        var client = _server.NewClient();

        var tokens = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => client.Tokens.GetTokenAsync(Api, CancellationToken.None))));

        tokens.Select(t => t.AccessToken).Should().AllBe("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenWaiterCancellationDoesNotCancelMint()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _server.Mint = async (n, body, _) =>
        {
            await release.Task;
            return FakeAuthServer.TokenResponse(body, $"token-{n}");
        };
        var client = _server.NewClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        var waiter = () => client.Tokens.GetTokenAsync(Api, cts.Token);
        await waiter.Should().ThrowAsync<OperationCanceledException>();

        release.SetResult();
        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        token.AccessToken.Should().Be("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenWaiterNotBlockedByStoreWrite()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new DelegatingTokenStore();
        store.OnSet = async (key, token, ttl, ct) =>
        {
            entered.TrySetResult();
            await release.Task;
            await store.Inner.SetAsync(key, token, ttl, ct);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store));
        try
        {
            using var first = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            _ = client.Tokens.GetTokenAsync(Api, first.Token);
            (await Task.WhenAny(entered.Task, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(entered.Task);

            using var second = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var other = client.Tokens.GetTokenAsync(VoiceIn, second.Token);
            (await Task.WhenAny(other, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(other);
            await ((Func<Task>)(() => other)).Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task TestTokenInvalidateBoundsStalledStore()
    {
        var stalled = false;
        var store = new DelegatingTokenStore();
        store.OnGet = async (key, ct) =>
        {
            if (Volatile.Read(ref stalled))
                await Task.Delay(Timeout.Infinite, ct);
            return await store.Inner.GetAsync(key, ct);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store));
        client.Tokens.StoreTimeout = TimeSpan.FromMilliseconds(50);
        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        Volatile.Write(ref stalled, true);
        var invalidate = client.Tokens.InvalidateAsync(Api, token.AccessToken, CancellationToken.None);
        (await Task.WhenAny(invalidate, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(invalidate);

        Volatile.Write(ref stalled, false);
        await client.Tokens.InvalidateAsync(Api, token.AccessToken, CancellationToken.None);
        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        _server.Mints.Should().Be(2);
    }

    [Theory]
    [InlineData("re-check read")]
    [InlineData("write")]
    [InlineData("write ignoring cancellation")]
    public async Task TestTokenFlightBoundsStalledStore(string stall)
    {
        var gets = 0;
        var store = new DelegatingTokenStore();
        store.OnGet = async (key, ct) =>
        {
            if (Interlocked.Increment(ref gets) == 2 && stall == "re-check read")
                await Task.Delay(Timeout.Infinite, ct);
            return await store.Inner.GetAsync(key, ct);
        };
        store.OnSet = async (key, token, ttl, ct) =>
        {
            if (stall == "write")
                await Task.Delay(Timeout.Infinite, ct);
            if (stall == "write ignoring cancellation")
                await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken.None);
            await store.Inner.SetAsync(key, token, ttl, ct);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store));
        client.Tokens.StoreTimeout = TimeSpan.FromMilliseconds(50);

        var mint = client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        (await Task.WhenAny(mint, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(mint);
        (await mint).AccessToken.Should().Be("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenInvalidateWaitsForStoreLockWithinCancellation()
    {
        var client = _server.NewClient();
        var storeLock = client.Tokens.StoreLock(client.Tokens.CacheKey(Api));
        await storeLock.WaitAsync();
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            var done = Task.Run(async () =>
            {
                await client.Tokens.InvalidateAsync(Api, "refused", cts.Token);
                await client.Tokens.InvalidateAsync(VoiceIn, "refused", CancellationToken.None);
            });
            (await Task.WhenAny(done, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(done);
        }
        finally
        {
            storeLock.Release();
        }
    }

    [Fact]
    public async Task TestTokenCanceledCallIsNotLoggedAsStoreFailure()
    {
        var gets = 0;
        var logger = new ListLogger(LogLevel.Warning);
        var store = new DelegatingTokenStore();
        store.OnGet = (_, ct) =>
        {
            Interlocked.Increment(ref gets);
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<DidwwToken?>(null);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store).SetLogger(logger));

        var act = () => client.Tokens.GetTokenAsync(Api, new CancellationToken(true));

        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Entries.Should().BeEmpty();
        gets.Should().Be(0);
        _server.Mints.Should().Be(0);
    }

    [Fact]
    public async Task TestTokenStoreReadIgnoringCancellationIsBoundedByCaller()
    {
        var release = new TaskCompletionSource<DidwwToken?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new ListLogger(LogLevel.Warning);
        var store = new DelegatingTokenStore { OnGet = (_, _) => new ValueTask<DidwwToken?>(release.Task) };
        var client = _server.NewClient(b => b.SetTokenStore(store).SetLogger(logger));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            var call = client.Tokens.GetTokenAsync(Api, cts.Token);

            (await Task.WhenAny(call, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(call);
            var act = () => call;
            await act.Should().ThrowAsync<OperationCanceledException>();
            logger.Entries.Should().BeEmpty();
            _server.Mints.Should().Be(0);
        }
        finally
        {
            release.SetResult(null);
        }
    }

    [Fact]
    public async Task TestTokenCancellationDuringStoreReadIsNotLogged()
    {
        var gets = 0;
        var logger = new ListLogger(LogLevel.Warning);
        using var cts = new CancellationTokenSource();
        var store = new DelegatingTokenStore();
        store.OnGet = (_, ct) =>
        {
            Interlocked.Increment(ref gets);
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<DidwwToken?>(null);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store).SetLogger(logger));

        var act = () => client.Tokens.GetTokenAsync(Api, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Entries.Should().BeEmpty();
        gets.Should().Be(1);
        _server.Mints.Should().Be(0);
    }

    [Fact]
    public async Task TestTokenStoreFailuresAreLoggedAndTokenStillReturned()
    {
        var logger = new ListLogger(LogLevel.Warning);
        var store = new DelegatingTokenStore
        {
            OnGet = (_, _) => throw new InvalidOperationException("store is broken"),
            OnSet = (_, _, _, _) => throw new InvalidOperationException("store is broken")
        };
        var client = _server.NewClient(b => b.SetTokenStore(store).SetLogger(logger));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-1");
        logger.Text.Should().Contain("DIDWW token store read failed");
        logger.Text.Should().Contain("DIDWW token store write failed");
        logger.Text.Should().NotContain("token-1");
    }

    [Fact]
    public async Task TestTokenLoggerFailureBecomesTheFlightResult()
    {
        var client = _server.NewClient(b => b.SetLogger(new ThrowingLogger()));

        for (var i = 0; i < 2; i++)
        {
            var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("logger is broken");
        }
        _server.Mints.Should().Be(2);
    }

    [Fact]
    public async Task TestTokenInvalidClient()
    {
        _server.Mint = (_, _, _) => Task.FromResult(TokenError(401, "token/invalid_client.json"));
        var client = _server.NewClient();
        var delays = RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.InvalidClient);
        e.StatusCode.Should().Be(401);
        e.Error.Should().Be("invalid_client");
        e.ErrorDescription.Should().Be("Client authentication failed.");
        e.Message.Should().Be("Token service: invalid_client: Client authentication failed.");
        e.Message.Should().NotContain(client.Credentials.ApiKey);
        _server.Mints.Should().Be(1);
        delays.All.Should().BeEmpty();
    }

    [Fact]
    public async Task TestTokenInvalidRequest()
    {
        _server.Mint = (_, _, _) => Task.FromResult(TokenError(400, "token/invalid_scope.json"));
        var client = _server.NewClient();

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.InvalidRequest);
        e.StatusCode.Should().Be(400);
        e.Error.Should().Be("invalid_scope");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestTokenRateLimitWindowIsSharedByClientsWithTheSameKey()
    {
        _server.Mint = (_, _, _) =>
        {
            var response = TokenError(429, "token/rate_limit_exceeded.json");
            response.Headers.TryAddWithoutValidation("Retry-After", "30");
            return Task.FromResult(response);
        };
        var first = _server.NewClient();
        var second = _server.NewClient(b => b.SetCredentials(first.Credentials));
        var unrelated = _server.NewClient();

        var act = () => first.Tokens.GetTokenAsync(Api, CancellationToken.None);
        await act.Should().ThrowAsync<DidwwAuthException>();

        act = () => second.Tokens.GetTokenAsync(Api, CancellationToken.None);
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        _server.Mints.Should().Be(1, "the window holds for every client of the process with this key");

        act = () => unrelated.Tokens.GetTokenAsync(Api, CancellationToken.None);
        await act.Should().ThrowAsync<DidwwAuthException>();
        _server.Mints.Should().Be(2, "another API key has its own window");
    }

    [Theory]
    [InlineData(0.0, 30)]
    [InlineData(1.0, 36)]
    public async Task TestTokenRateLimited(double jitter, int windowSeconds)
    {
        _server.Mint = (n, body, _) =>
        {
            if (n > 1)
                return Task.FromResult(FakeAuthServer.TokenResponse(body, $"token-{n}"));
            var response = TokenError(429, "token/rate_limit_exceeded.json");
            response.Headers.TryAddWithoutValidation("Retry-After", "30");
            return Task.FromResult(response);
        };
        var client = _server.NewClient();
        var clock = new ManualTimeProvider();
        client.Tokens.TimeProvider = clock;
        var delays = RecordedDelays.Stub(client, jitter);

        var first = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        var e = (await first.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        e.StatusCode.Should().Be(429);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(30), "the server value, without the spread");

        clock.Advance(TimeSpan.FromSeconds(windowSeconds) - TimeSpan.FromMilliseconds(500));
        e = (await first.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        e.Error.Should().Be("rate_limit_exceeded");
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(1));

        var other = () => client.Tokens.GetTokenAsync(VoiceIn, CancellationToken.None);
        (await other.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        _server.Mints.Should().Be(1);

        clock.Advance(TimeSpan.FromMilliseconds(500));
        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        token.AccessToken.Should().Be("token-2");
        _server.Mints.Should().Be(2);
        delays.All.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.0, 1)]
    [InlineData(1.0, 2)]
    public async Task TestTokenRateLimitedWindowForZeroRetryAfter(double jitter, int windowSeconds)
    {
        _server.Mint = (n, body, _) =>
        {
            if (n > 1)
                return Task.FromResult(FakeAuthServer.TokenResponse(body, "token-ok"));
            var response = TokenError(429, "token/rate_limit_exceeded.json");
            response.Headers.TryAddWithoutValidation("Retry-After", "0");
            return Task.FromResult(response);
        };
        var client = _server.NewClient();
        var clock = new ManualTimeProvider();
        client.Tokens.TimeProvider = clock;
        RecordedDelays.Stub(client, jitter);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(1));

        clock.Advance(TimeSpan.FromSeconds(windowSeconds) - TimeSpan.FromMilliseconds(1));
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        (await client.Tokens.GetTokenAsync(Api, CancellationToken.None)).AccessToken.Should().Be("token-ok");
        _server.Mints.Should().Be(2);
    }

    [Theory]
    [InlineData("Wed, 21 Oct 2026 07:28:00 GMT")]
    [InlineData(null)]
    public async Task TestTokenRateLimitedWithoutRetryAfterSeconds(string? retryAfter)
    {
        _server.Mint = (_, _, _) =>
        {
            var response = TokenError(429, "token/rate_limit_exceeded.json");
            if (retryAfter != null)
                response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
            return Task.FromResult(response);
        };
        var client = _server.NewClient();

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(1.0, new[] { 1000, 2000, 4000 })]
    [InlineData(0.5, new[] { 500, 1000, 2000 })]
    public async Task TestTokenServerErrorRetriedWithBackoff(double jitter, int[] expectedMilliseconds)
    {
        _server.Mint = (n, body, _) => Task.FromResult(n < 4
            ? TokenError(503, "token/server_error.json")
            : FakeAuthServer.TokenResponse(body, "token-ok"));
        var client = _server.NewClient();
        var delays = RecordedDelays.Stub(client, jitter);

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-ok");
        _server.Mints.Should().Be(4);
        delays.All.Should().Equal(expectedMilliseconds.Select(ms => TimeSpan.FromMilliseconds(ms)));
    }

    [Fact]
    public void TestTokenBackoffCap()
    {
        var client = _server.NewClient();
        client.Tokens.NextDouble = () => 1;

        client.Tokens.Backoff(1).Should().Be(TimeSpan.FromSeconds(1));
        client.Tokens.Backoff(3).Should().Be(TimeSpan.FromSeconds(4));
        client.Tokens.Backoff(4).Should().Be(TimeSpan.FromSeconds(8));
        client.Tokens.Backoff(5).Should().Be(TimeSpan.FromSeconds(8));
        client.Tokens.Backoff(100).Should().Be(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void TestSpreadRetryAfter()
    {
        TokenProvider.SpreadRetryAfter(TimeSpan.FromSeconds(1), () => 0).Should().Be(TimeSpan.FromSeconds(1));
        TokenProvider.SpreadRetryAfter(TimeSpan.FromSeconds(1), () => 1).Should().Be(TimeSpan.FromSeconds(2));
        TokenProvider.SpreadRetryAfter(TimeSpan.FromSeconds(30), () => 0).Should().Be(TimeSpan.FromSeconds(30));
        TokenProvider.SpreadRetryAfter(TimeSpan.FromSeconds(30), () => 1).Should().Be(TimeSpan.FromSeconds(36));
        TokenProvider.SpreadRetryAfter(TimeSpan.FromSeconds(30), () => 0.5).Should().Be(TimeSpan.FromSeconds(33));
    }

    [Fact]
    public async Task TestTokenServerErrorExhausted()
    {
        _server.Mint = (_, _, _) => Task.FromResult(TokenError(500, "token/server_error.json"));
        var client = _server.NewClient();
        var delays = RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.ServerError);
        e.StatusCode.Should().Be(500);
        e.Error.Should().Be("server_error");
        _server.Mints.Should().Be(TokenProvider.MaxMintAttempts);
        delays.All.Should().HaveCount(TokenProvider.MaxMintAttempts - 1);
    }

    [Fact]
    public async Task TestTokenServerErrorWithoutJsonBody()
    {
        _server.Mint = (_, _, _) => Task.FromResult(FakeAuthServer.Json(502, "<html>Bad Gateway</html>", "text/html"));
        var client = _server.NewClient();
        RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.ServerError);
        e.StatusCode.Should().Be(502);
        e.Error.Should().BeNull();
        e.Message.Should().Be("Token service responded with status 502");
    }

    [Fact]
    public async Task TestTokenUnexpectedStatus()
    {
        _server.Mint = (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = _server.NewClient();
        var delays = RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.UnexpectedStatus);
        e.StatusCode.Should().Be(403);
        _server.Mints.Should().Be(1);
        delays.All.Should().BeEmpty();
    }

    [Fact]
    public async Task TestTokenLaterCallMintsAgainAfterFailure()
    {
        _server.Mint = (n, body, _) => Task.FromResult(n <= TokenProvider.MaxMintAttempts
            ? TokenError(500, "token/server_error.json")
            : FakeAuthServer.TokenResponse(body, "token-ok"));
        var client = _server.NewClient();
        RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.ServerError);

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);
        token.AccessToken.Should().Be("token-ok");
        _server.Mints.Should().Be(TokenProvider.MaxMintAttempts + 1);
    }

    [Fact]
    public async Task TestTokenConnectionError()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("key-" + Guid.NewGuid().ToString("N"), DidwwEnvironment.Sandbox))
            .SetAuthUrl($"http://127.0.0.1:{port}")
            .SetLogger(null)
            .Build();
        var delays = RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.ConnectionError);
        e.StatusCode.Should().BeNull();
        e.InnerException.Should().NotBeNull();
        delays.All.Should().HaveCount(TokenProvider.MaxMintAttempts - 1);
    }

    [Fact]
    public async Task TestTokenTlsErrorNotRetried()
    {
        _server.Mint = (_, _, _) => throw new HttpRequestException(HttpRequestError.SecureConnectionError,
            "The SSL connection could not be established", new AuthenticationException("certificate rejected"));
        var client = _server.NewClient();
        var delays = RecordedDelays.Stub(client, 1);

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.ConnectionError);
        TokenProvider.IsTlsError(e.InnerException!).Should().BeTrue();
        _server.Mints.Should().Be(1);
        delays.All.Should().BeEmpty();
    }

    [Fact]
    public async Task TestTokenTlsAlertNotRetried()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var connections = 0;
        using var stop = new CancellationTokenSource();
        var accepting = Task.Run(async () =>
        {
            while (!stop.IsCancellationRequested)
            {
                TcpClient connection;
                try
                {
                    connection = await listener.AcceptTcpClientAsync(stop.Token);
                }
                catch (System.Exception)
                {
                    return;
                }
                Interlocked.Increment(ref connections);
                _ = Task.Run(async () =>
                {
                    using (connection)
                    {
                        var stream = connection.GetStream();
                        // A fatal handshake_failure alert, then wait for the client to close.
                        await stream.WriteAsync(new byte[] { 0x15, 0x03, 0x03, 0x00, 0x02, 0x02, 0x28 });
                        using var wait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var buffer = new byte[4096];
                        try
                        {
                            while (await stream.ReadAsync(buffer, wait.Token) > 0)
                            {
                            }
                        }
                        catch (System.Exception)
                        {
                            // The client went away.
                        }
                    }
                });
            }
        });
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials("key-" + Guid.NewGuid().ToString("N"), DidwwEnvironment.Sandbox))
                .SetAuthUrl($"https://127.0.0.1:{port}")
                .SetLogger(null)
                .Build();
            var delays = RecordedDelays.Stub(client, 1);

            var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

            var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
            e.Kind.Should().Be(DidwwAuthErrorKind.ConnectionError);
            TokenProvider.IsTlsError(e.InnerException!).Should().BeTrue();
            delays.All.Should().BeEmpty();
            Volatile.Read(ref connections).Should().Be(1);
        }
        finally
        {
            stop.Cancel();
            listener.Stop();
            await accepting;
        }
    }

    [Theory]
    [InlineData("""{"access_token":"t","token_type":"Bearer","expires_in":900,"aud":["api-sms.didww.com"],"scope":"full","api_version":"2026-10-22"}""")]
    [InlineData("""{"access_token":"","token_type":"Bearer","expires_in":900,"aud":["api.didww.com"],"scope":"full","api_version":"2026-10-22"}""")]
    [InlineData("""{"access_token":"t","token_type":"Bearer","expires_in":900,"aud":"api.didww.com"}""")]
    [InlineData("ok")]
    public async Task TestTokenInvalidGrant(string body)
    {
        _server.Mint = (_, _, _) => Task.FromResult(FakeAuthServer.Json(200, body));
        var client = _server.NewClient();

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.InvalidGrant);
        e.StatusCode.Should().Be(200);
        e.Message.Should().Contain("requested api.didww.com");
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.InvalidGrant);
        _server.Mints.Should().Be(2);
    }

    [Fact]
    public async Task TestTokenScopeMismatchWarning()
    {
        var logger = new ListLogger(LogLevel.Warning);
        var client = _server.NewClient(b => b.SetScope(DidwwScope.Read).SetLogger(logger));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.Scope.Should().Be("full");
        logger.Text.Should().Contain("Warning: DIDWW token scope mismatch: requested read, granted full");
        logger.Text.Should().NotContain("token-1");
    }

    [Fact]
    public async Task TestTokenApiVersionMismatchWarning()
    {
        _server.Mint = (_, _, _) => Task.FromResult(FakeAuthServer.Json(200,
            """{"access_token":"t","token_type":"Bearer","expires_in":900,"aud":["api.didww.com"],"scope":"full","api_version":"2099-01-01"}"""));
        var logger = new ListLogger(LogLevel.Warning);
        var client = _server.NewClient(b => b.SetLogger(logger));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("t");
        logger.Text.Should().Contain("Warning: DIDWW token api_version mismatch: requested 2026-10-22, granted 2099-01-01");
    }

    [Fact]
    public async Task TestTokenNoWarningWhenGrantMatches()
    {
        var logger = new ListLogger(LogLevel.Warning);
        var client = _server.NewClient(b => b.SetScope(DidwwScope.Full).SetLogger(logger));

        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task TestTokenNoScopeWarningWithoutRequestedScope()
    {
        var logger = new ListLogger(LogLevel.Warning);
        var client = _server.NewClient(b => b.SetLogger(logger));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.Scope.Should().Be("full");
        logger.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task TestTokenMintedDebugLog()
    {
        var logger = new ListLogger(LogLevel.Debug);
        var client = _server.NewClient(b => b.SetLogger(logger));

        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        logger.Text.Should().Contain(
            "Debug: DIDWW token minted: aud api.didww.com, scope full, api_version 2026-10-22, expires_in 900");
        logger.Text.Should().NotContain("token-1");
        logger.Text.Should().NotContain(client.Credentials.ApiKey);
    }

    [Fact]
    public void TestDefaultLoggerIsSilent()
    {
        var client = DidwwClient.NewBuilder()
            .SetCredentials(new DidwwCredentials("test-api-key", DidwwEnvironment.Sandbox))
            .Build();

        client.Logger.Should().BeSameAs(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
    }

    [Fact]
    public async Task TestTokenInvalidateEvictsTheRefusedToken()
    {
        var client = _server.NewClient();
        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        await client.Tokens.InvalidateAsync(Api, "token-1", CancellationToken.None);

        (await client.Tokens.GetTokenAsync(Api, CancellationToken.None)).AccessToken.Should().Be("token-2");
    }

    [Fact]
    public async Task TestTokenInvalidateKeepsATokenMintedAfterTheRefusedOne()
    {
        var client = _server.NewClient();
        await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        await client.Tokens.InvalidateAsync(Api, "stale-token", CancellationToken.None);

        (await client.Tokens.GetTokenAsync(Api, CancellationToken.None)).AccessToken.Should().Be("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public void TestAuthExceptionMessage()
    {
        var transport = new HttpRequestException("connection refused");

        new DidwwAuthException(DidwwAuthErrorKind.InvalidRequest, 400, "invalid_request", "aud is missing").Message
            .Should().Be("Token service: invalid_request: aud is missing");
        new DidwwAuthException(DidwwAuthErrorKind.RateLimited, 429, "rate_limit_exceeded").Message
            .Should().Be("Token service: rate_limit_exceeded");
        new DidwwAuthException(DidwwAuthErrorKind.InvalidGrant, 200,
                errorDescription: "granted audience [], requested api.didww.com").Message
            .Should().Be("Token service: granted audience [], requested api.didww.com");
        new DidwwAuthException(DidwwAuthErrorKind.ConnectionError, innerException: transport).Message
            .Should().Be("Token service: connection refused");
        new DidwwAuthException(DidwwAuthErrorKind.ServerError, 503).Message
            .Should().Be("Token service responded with status 503");
        new DidwwAuthException(DidwwAuthErrorKind.ConnectionError, innerException: transport).InnerException
            .Should().BeSameAs(transport);
        new DidwwAuthException(DidwwAuthErrorKind.ServerError, 503).Should().BeAssignableTo<DidwwException>();
    }

    [Fact]
    public void TestIsTlsError()
    {
        TokenProvider.IsTlsError(new HttpRequestException(HttpRequestError.SecureConnectionError, "tls")).Should().BeTrue();
        TokenProvider.IsTlsError(new HttpRequestException("proxy", new HttpRequestException("inner",
            new AuthenticationException("alert")))).Should().BeTrue();
        TokenProvider.IsTlsError(new HttpRequestException(HttpRequestError.ConnectionError, "refused",
            new SocketException((int)SocketError.ConnectionRefused))).Should().BeFalse();
        TokenProvider.IsTlsError(new TaskCanceledException("timeout")).Should().BeFalse();
    }

    [Fact]
    public async Task TestTokenMintTimeoutRetried()
    {
        _server.Mint = async (n, body, ct) =>
        {
            if (n == 1)
                await Task.Delay(Timeout.Infinite, ct);
            return FakeAuthServer.TokenResponse(body, "token-2");
        };
        var client = _server.NewClient();
        client.Tokens.MintTimeout = TimeSpan.FromMilliseconds(200);
        var delays = RecordedDelays.Stub(client, 1);

        var mint = client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        (await Task.WhenAny(mint, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(mint);
        (await mint).AccessToken.Should().Be("token-2");
        _server.Mints.Should().Be(2);
        delays.All.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestTokenMintTimeoutExhaustsAttempts()
    {
        _server.Mint = async (_, _, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable");
        };
        var client = _server.NewClient();
        client.Tokens.MintTimeout = TimeSpan.FromMilliseconds(100);
        RecordedDelays.Stub(client, 1);

        var mint = client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        (await Task.WhenAny(mint, Task.Delay(TimeSpan.FromSeconds(5)))).Should().Be(mint);
        var act = () => mint;
        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.ConnectionError);
        _server.Mints.Should().Be(TokenProvider.MaxMintAttempts);
    }

    [Fact]
    public async Task TestTokenFlightRereadsStoreBeforeMinting()
    {
        var seeded = 0;
        var store = new DelegatingTokenStore();
        store.OnGet = async (key, ct) =>
        {
            if (Interlocked.Exchange(ref seeded, 1) == 0)
            {
                // Another process sharing the store stores a token meanwhile.
                await store.Inner.SetAsync(key, new DidwwToken { AccessToken = "shared-token", ExpiresIn = 60 },
                    TimeSpan.FromMinutes(1), ct);
                return null;
            }
            return await store.Inner.GetAsync(key, ct);
        };
        var client = _server.NewClient(b => b.SetTokenStore(store));

        var token = await client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        token.AccessToken.Should().Be("shared-token");
        _server.Mints.Should().Be(0);
    }

    [Fact]
    public async Task TestTokenRetryStopsInsideRateLimitWindow()
    {
        _server.Mint = (_, _, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = _server.NewClient();
        client.Tokens.Delay = (_, _) =>
        {
            // Another audience's mint is answered 429 while this one backs off.
            client.Tokens.BlockFor(TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        };

        var act = () => client.Tokens.GetTokenAsync(Api, CancellationToken.None);

        var e = (await act.Should().ThrowAsync<DidwwAuthException>()).Which;
        e.Kind.Should().Be(DidwwAuthErrorKind.RateLimited);
        e.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
        _server.Mints.Should().Be(1);
    }

    private sealed class ThrowingLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception,
            Func<TState, System.Exception?, string> formatter) =>
            throw new InvalidOperationException("logger is broken");
    }
}
