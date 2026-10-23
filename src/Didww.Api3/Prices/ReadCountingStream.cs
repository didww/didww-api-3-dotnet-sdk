namespace Didww.Api3.Prices;

/// <summary>
/// A read-only pass-through stream that counts the bytes read and keeps the
/// last eight of them. <c>atEnd</c>, when given, runs each time a read reaches
/// the end of the inner stream and may throw. The inner stream is never disposed.
/// </summary>
internal sealed class ReadCountingStream : Stream
{
    private readonly Stream _inner;
    private readonly Action<ReadCountingStream>? _atEnd;
    private readonly byte[] _tail = new byte[8];

    internal ReadCountingStream(Stream inner, Action<ReadCountingStream>? atEnd = null)
    {
        _inner = inner;
        _atEnd = atEnd;
    }

    /// <summary>The number of bytes read.</summary>
    internal long Count { get; private set; }

    /// <summary>The last eight bytes read (leading zeros while fewer were read).</summary>
    internal ReadOnlySpan<byte> Tail => _tail;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var read = _inner.Read(buffer);
        return Track(buffer[..read], buffer.Length);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Track(buffer.Span[..read], buffer.Length);
    }

    private int Track(ReadOnlySpan<byte> read, int requested)
    {
        if (read.Length == 0)
        {
            if (requested > 0)
                _atEnd?.Invoke(this);
            return 0;
        }

        Count += read.Length;
        if (read.Length >= _tail.Length)
        {
            read[^_tail.Length..].CopyTo(_tail);
        }
        else
        {
            _tail.AsSpan(read.Length).CopyTo(_tail);
            read.CopyTo(_tail.AsSpan(_tail.Length - read.Length));
        }
        return read.Length;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
