namespace Didww.Api3.Auth;

/// <summary>
/// Caches access tokens. Keys are opaque strings that already cover the
/// token service URL, the API key (hashed), the audience, the API version and the
/// scope, so one store may be shared by several clients or processes.
/// </summary>
/// <remarks>
/// A store holds live bearer tokens: protect it like the API key. A token is
/// stored for the lifetime granted by the token service; it is refreshed when
/// the API refuses it (401), not ahead of its expiry.
/// Implementations should honour the cancellation token: the SDK bounds the
/// store calls it makes on its own behalf with a timeout.
/// </remarks>
public interface ITokenStore
{
    /// <summary>Returns the token stored under <paramref name="key"/>, or null when there is none or it has expired.</summary>
    ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>Stores <paramref name="token"/> under <paramref name="key"/> for <paramref name="ttl"/>; a ttl of zero or less means no expiry.</summary>
    ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>Removes the token stored under <paramref name="key"/>, if any.</summary>
    ValueTask DeleteAsync(string key, CancellationToken cancellationToken);
}
