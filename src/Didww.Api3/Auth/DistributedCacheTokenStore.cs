using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Didww.Api3.Auth;

/// <summary>
/// An <see cref="ITokenStore"/> backed by an <see cref="IDistributedCache"/>, such as
/// Redis or SQL Server, so that clients in several processes or instances share
/// tokens instead of each minting its own.
/// </summary>
/// <remarks>
/// Tokens are stored as JSON under the SDK's keys, each for the ttl it is set
/// with. An entry that cannot be read as a token makes <see cref="GetAsync"/>
/// throw; the client then logs a warning and mints a new token over it.
/// </remarks>
public sealed class DistributedCacheTokenStore : ITokenStore
{
    private readonly IDistributedCache _cache;

    public DistributedCacheTokenStore(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    public async ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        return value == null ? null : JsonSerializer.Deserialize<DidwwToken>(value);
    }

    public async ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);
        var options = new DistributedCacheEntryOptions();
        if (ttl > TimeSpan.Zero)
            options.AbsoluteExpirationRelativeToNow = ttl;
        await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(token), options, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask DeleteAsync(string key, CancellationToken cancellationToken) =>
        await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);

    /// <summary>Describes the store without its tokens.</summary>
    public override string ToString() => $"DistributedCacheTokenStore{{cache: {_cache.GetType().Name}}}";
}
