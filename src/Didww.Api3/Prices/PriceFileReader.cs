using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace Didww.Api3.Prices;

/// <summary>
/// Reads a price file as downloaded (gzipped CSV), one row at a time.
/// </summary>
/// <remarks>
/// Each row is keyed by the header line, as is (for example
/// <c>Rate (USD, excl. VAT)</c>); a UTF-8 byte order mark is skipped and a
/// file with only a header has no rows. The input is a single gzip member, as
/// the prices services serve it. Input that is not gzip, gzip data cut short
/// (an empty input included), or a row whose field count differs from the
/// header, raises <see cref="InvalidDataException"/>. The rows are read lazily
/// while enumerating, so rows before the error have already been returned; the
/// stream is left open.
/// </remarks>
public static class PriceFileReader
{
    /// <summary>Enumerates the rows of the gzipped price file in <paramref name="gzipped"/>.</summary>
    public static IEnumerable<IReadOnlyDictionary<string, string>> ReadRows(Stream gzipped)
    {
        ArgumentNullException.ThrowIfNull(gzipped);
        return ReadRowsIterator(gzipped);
    }

    /// <summary>Enumerates the rows of the gzipped price file in <paramref name="gzipped"/> asynchronously.</summary>
    public static IAsyncEnumerable<IReadOnlyDictionary<string, string>> ReadRowsAsync(Stream gzipped,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gzipped);
        return ReadRowsAsyncIterator(gzipped, cancellationToken);
    }

    private static IEnumerable<IReadOnlyDictionary<string, string>> ReadRowsIterator(Stream gzipped)
    {
        var source = new ReadCountingStream(gzipped);
        using var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        using var text = NewTextReader(Checked(gzip, source));
        var csv = new CsvRecordReader(text);

        var header = csv.Read();
        if (header == null)
            yield break;
        while (csv.Read() is { } record)
            yield return ToRow(header, record, csv.RecordLine);
    }

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, string>> ReadRowsAsyncIterator(Stream gzipped,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var source = new ReadCountingStream(gzipped);
        var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
        await using (gzip.ConfigureAwait(false))
        {
            using var text = NewTextReader(Checked(gzip, source));
            var csv = new CsvRecordReader(text);

            var header = await csv.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (header == null)
                yield break;
            while (await csv.ReadAsync(cancellationToken).ConfigureAwait(false) is { } record)
                yield return ToRow(header, record, csv.RecordLine);
        }
    }

    // The decompressed data of gzip, which reads from source. At its end the gzip
    // member must have ended with its trailer: GZipStream checks the CRC-32 of a
    // trailer it reads but returns end of data when the input stops early. A price
    // file is one gzip member, so the input ends with the trailer: a header of at
    // least 10 bytes, then the CRC-32 and ISIZE (the data length mod 2^32).
    private static ReadCountingStream Checked(GZipStream gzip, ReadCountingStream source) =>
        new(gzip, data =>
        {
            if (source.Count < 18 ||
                BinaryPrimitives.ReadUInt32LittleEndian(source.Tail[4..]) != (uint)data.Count)
                throw new InvalidDataException("Price file: unexpected end of gzip data");
        });

    // UTF-8, invalid bytes read as U+FFFD. An encoding with a preamble makes the
    // reader skip a leading UTF-8 byte order mark.
    private static StreamReader NewTextReader(Stream stream) =>
        new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), detectEncodingFromByteOrderMarks: false,
            bufferSize: 16 * 1024, leaveOpen: true);

    private static Dictionary<string, string> ToRow(List<string> header, List<string> record, int line)
    {
        if (record.Count != header.Count)
            throw new InvalidDataException($"Price file: record on line {line}: wrong number of fields");
        var row = new Dictionary<string, string>(header.Count, StringComparer.Ordinal);
        for (var i = 0; i < header.Count; i++)
            row[header[i]] = record[i];
        return row;
    }
}
