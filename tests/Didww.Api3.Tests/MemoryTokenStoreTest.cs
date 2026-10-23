using Didww.Api3.Auth;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class MemoryTokenStoreTest
{
    private static DidwwToken Token(string accessToken) => new()
    {
        AccessToken = accessToken,
        TokenType = "Bearer",
        ExpiresIn = 900,
        Audience = new[] { "api.didww.com" },
        Scope = "full",
        ApiVersion = "2026-10-22"
    };

    [Fact]
    public async Task TestGetMissing()
    {
        var store = new MemoryTokenStore();

        (await store.GetAsync("missing", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task TestExpiry()
    {
        var clock = new ManualTimeProvider();
        var store = new MemoryTokenStore(clock);
        await store.SetAsync("key", Token("secret"), TimeSpan.FromSeconds(60), CancellationToken.None);

        clock.Advance(TimeSpan.FromSeconds(59));
        (await store.GetAsync("key", CancellationToken.None))!.AccessToken.Should().Be("secret");

        clock.Advance(TimeSpan.FromSeconds(1));
        (await store.GetAsync("key", CancellationToken.None)).Should().BeNull();
        store.ToString().Should().Be("MemoryTokenStore{entries: 0}");
    }

    [Fact]
    public async Task TestWithoutTtl()
    {
        var clock = new ManualTimeProvider();
        var store = new MemoryTokenStore(clock);
        await store.SetAsync("key", Token("secret"), TimeSpan.Zero, CancellationToken.None);

        clock.Advance(TimeSpan.FromDays(365));

        (await store.GetAsync("key", CancellationToken.None))!.AccessToken.Should().Be("secret");
    }

    [Fact]
    public async Task TestReplace()
    {
        var store = new MemoryTokenStore();
        await store.SetAsync("key", Token("first"), TimeSpan.FromMinutes(1), CancellationToken.None);

        await store.SetAsync("key", Token("second"), TimeSpan.FromMinutes(1), CancellationToken.None);

        (await store.GetAsync("key", CancellationToken.None))!.AccessToken.Should().Be("second");
    }

    [Fact]
    public async Task TestDelete()
    {
        var store = new MemoryTokenStore();
        await store.SetAsync("key", Token("secret"), TimeSpan.FromMinutes(1), CancellationToken.None);

        await store.DeleteAsync("key", CancellationToken.None);
        await store.DeleteAsync("missing", CancellationToken.None);

        (await store.GetAsync("key", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task TestToStringHidesTokens()
    {
        var store = new MemoryTokenStore();
        await store.SetAsync("key", Token("secret-access-token"), TimeSpan.FromMinutes(1), CancellationToken.None);

        store.ToString().Should().Be("MemoryTokenStore{entries: 1}");
        store.ToString().Should().NotContain("secret-access-token");
    }
}
