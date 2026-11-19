namespace Didww.Api3.Prices;

/// <summary>
/// The price file metadata of a HEAD or download answer.
/// </summary>
public sealed class PriceFile
{
    internal PriceFile(int statusCode, DateTimeOffset? lastModified, long? contentLength, string? fileName,
        long bytesWritten)
    {
        StatusCode = statusCode;
        LastModified = lastModified;
        ContentLength = contentLength;
        FileName = fileName;
        BytesWritten = bytesWritten;
    }

    /// <summary>
    /// 200, or 304 when the file has not changed since the If-Modified-Since
    /// date sent.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>The Last-Modified of the file; null when absent.</summary>
    public DateTimeOffset? LastModified { get; }

    /// <summary>The size of the file in bytes; null when unknown, for example on 304.</summary>
    public long? ContentLength { get; }

    /// <summary>The file name from Content-Disposition, for example <c>voice_in_prices.csv.gz</c>.</summary>
    public string? FileName { get; }

    /// <summary>The bytes written by a download; 0 for HEAD and 304.</summary>
    public long BytesWritten { get; }

    /// <summary>Whether the file has not changed since the If-Modified-Since date sent (304).</summary>
    public bool NotModified => StatusCode == 304;

    public override string ToString() =>
        $"PriceFile(StatusCode={StatusCode}, LastModified={LastModified:R}, ContentLength={ContentLength}, " +
        $"FileName={FileName}, BytesWritten={BytesWritten})";
}
