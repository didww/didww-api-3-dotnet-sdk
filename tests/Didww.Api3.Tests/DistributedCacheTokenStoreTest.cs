using System.Text;
using System.Text.Json;
using Didww.Api3.Auth;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;

namespace Didww.Api3.Tests;

public class DistributedCacheTokenStoreTest : IDisposable
{
    private const string Key = "didww:token:v1:7f410b6b";

    private readonly FakeAuthServer _server = new();

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    private static DidwwToken Token(string accessToken) => new()
    {
        AccessToken = accessToken,
        TokenType = "Bearer",
        ExpiresIn = 900,
        Audience = new[] { "api.didww.com" },
        Scope = "full",
        ApiVersion = "2026-11-19",
        MintedAt = new DateTimeOffset(2026, 11, 19, 10, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task TestRoundTrip()
    {
        var cache = new RecordingCache();
        var store = new DistributedCacheTokenStore(cache);
        var token = Token("secret");

        await store.SetAsync(Key, token, TimeSpan.FromSeconds(900), CancellationToken.None);

        var entry = cache.Entries[Key];
        entry.Options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromSeconds(900));
        entry.Options.AbsoluteExpiration.Should().BeNull();
        entry.Options.SlidingExpiration.Should().BeNull();
        JsonDocument.Parse(entry.Value).RootElement.GetProperty("access_token").GetString().Should().Be("secret");
        (await store.GetAsync(Key, CancellationToken.None)).Should().BeEquivalentTo(token);
    }

    [Fact]
    public async Task TestWithoutTtl()
    {
        var cache = new RecordingCache();
        var store = new DistributedCacheTokenStore(cache);

        await store.SetAsync(Key, Token("secret"), TimeSpan.Zero, CancellationToken.None);

        cache.Entries[Key].Options.AbsoluteExpirationRelativeToNow.Should().BeNull();
    }

    [Fact]
    public async Task TestGetMissing()
    {
        var store = new DistributedCacheTokenStore(new RecordingCache());

        (await store.GetAsync(Key, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task TestUnreadableEntryThrows()
    {
        var cache = new RecordingCache();
        cache.Set(Key, Encoding.UTF8.GetBytes("garbage"), new DistributedCacheEntryOptions());
        var store = new DistributedCacheTokenStore(cache);

        var get = async () => await store.GetAsync(Key, CancellationToken.None);

        await get.Should().ThrowAsync<JsonException>();
    }

    [Fact]
    public async Task TestDelete()
    {
        var cache = new RecordingCache();
        var store = new DistributedCacheTokenStore(cache);
        await store.SetAsync(Key, Token("secret"), TimeSpan.FromMinutes(1), CancellationToken.None);

        await store.DeleteAsync(Key, CancellationToken.None);

        cache.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task TestCancellationTokenReachesCache()
    {
        var cache = new RecordingCache();
        var store = new DistributedCacheTokenStore(cache);
        using var cts = new CancellationTokenSource();

        await store.SetAsync(Key, Token("secret"), TimeSpan.FromMinutes(1), cts.Token);
        await store.GetAsync(Key, cts.Token);
        await store.DeleteAsync(Key, cts.Token);

        cache.Tokens.Should().Equal(cts.Token, cts.Token, cts.Token);
    }

    [Fact]
    public void TestNullCache()
    {
        var create = () => new DistributedCacheTokenStore(null!);

        create.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task TestToStringHidesTokens()
    {
        var store = new DistributedCacheTokenStore(new RecordingCache());
        await store.SetAsync(Key, Token("secret-access-token"), TimeSpan.FromMinutes(1), CancellationToken.None);

        store.ToString().Should().Be("DistributedCacheTokenStore{cache: RecordingCache}");
    }

    [Fact]
    public async Task TestTokenSharedBetweenProcesses()
    {
        var cache = new RecordingCache();
        var apiKey = "key-" + Guid.NewGuid().ToString("N");
        DidwwClient NewClient() => _server.NewClient(b => b
            .SetCredentials(new DidwwCredentials(apiKey, DidwwEnvironment.Sandbox))
            .SetTokenStore(new DistributedCacheTokenStore(cache)));

        await NewClient().Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);
        var token = await NewClient().Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-1");
        _server.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestUnreadableEntryReplacedByClient()
    {
        var cache = new RecordingCache();
        var logger = new ListLogger();
        var client = _server.NewClient(b => b.SetTokenStore(new DistributedCacheTokenStore(cache)).SetLogger(logger));
        var key = client.Tokens.CacheKey(TokenAudience.Api);
        cache.Set(key, Encoding.UTF8.GetBytes("garbage"), new DistributedCacheEntryOptions());

        var token = await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-1");
        logger.Text.Should().Contain("DIDWW token store read failed");
        JsonDocument.Parse(cache.Entries[key].Value).RootElement.GetProperty("access_token").GetString()
            .Should().Be("token-1");
    }

    [Fact]
    public async Task TestRefusedTokenReplaced()
    {
        var cache = new RecordingCache();
        var client = _server.NewClient(b => b.SetTokenStore(new DistributedCacheTokenStore(cache)));
        var refused = await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);

        await client.Tokens.InvalidateAsync(TokenAudience.Api, refused.AccessToken, CancellationToken.None);
        var token = await client.Tokens.GetTokenAsync(TokenAudience.Api, CancellationToken.None);

        token.AccessToken.Should().Be("token-2");
        cache.Entries.Should().ContainSingle();
    }

    private sealed class RecordingCache : IDistributedCache
    {
        public Dictionary<string, (byte[] Value, DistributedCacheEntryOptions Options)> Entries { get; } = new();

        public List<CancellationToken> Tokens { get; } = new();

        public byte[]? Get(string key) => Entries.TryGetValue(key, out var entry) ? entry.Value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            Tokens.Add(token);
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Entries[key] = (value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Tokens.Add(token);
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => Entries.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Tokens.Add(token);
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
