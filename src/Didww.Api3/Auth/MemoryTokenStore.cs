namespace Didww.Api3.Auth;

/// <summary>
/// An <see cref="ITokenStore"/> that keeps tokens in process memory. Expired
/// entries are dropped when read. It is safe for concurrent use and is the
/// default store of <see cref="DidwwClient"/>.
/// </summary>
public sealed class MemoryTokenStore : ITokenStore
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Entry> _entries = new();
    private readonly TimeProvider _timeProvider;

    public MemoryTokenStore() : this(TimeProvider.System)
    {
    }

    internal MemoryTokenStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(key, out var entry))
                return ValueTask.FromResult<DidwwToken?>(null);
            if (entry.ExpiresAt.HasValue && _timeProvider.GetTimestamp() >= entry.ExpiresAt.Value)
            {
                _entries.Remove(key);
                return ValueTask.FromResult<DidwwToken?>(null);
            }
            return ValueTask.FromResult<DidwwToken?>(entry.Token);
        }
    }

    public ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(token);
        long? expiresAt = null;
        if (ttl > TimeSpan.Zero)
            expiresAt = _timeProvider.GetTimestamp() + (long)(ttl.TotalSeconds * _timeProvider.TimestampFrequency);

        lock (_lock)
        {
            _entries[key] = new Entry(token, expiresAt);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteAsync(string key, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _entries.Remove(key);
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>Describes the store without its tokens.</summary>
    public override string ToString()
    {
        lock (_lock)
        {
            return $"MemoryTokenStore{{entries: {_entries.Count}}}";
        }
    }

    private readonly record struct Entry(DidwwToken Token, long? ExpiresAt);
}
