using System.IO.Compression;
using System.Text;
using Didww.Api3.Prices;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class PricesFileReaderTest
{
    private static MemoryStream Fixture(string name) => new(PricesServer.LoadBytes("prices/" + name));

    // A complete gzip member with no data; GZipStream writes nothing for an empty input.
    private static readonly byte[] EmptyGzip = Convert.FromHexString("1f8b08000000000002ff03000000000000000000");

    private static MemoryStream Gzip(string text)
    {
        if (text.Length == 0)
            return new MemoryStream(EmptyGzip);
        var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionMode.Compress, leaveOpen: true))
            gzip.Write(Encoding.UTF8.GetBytes(text));
        buffer.Position = 0;
        return buffer;
    }

    private static async Task<List<IReadOnlyDictionary<string, string>>> ReadAllAsync(Stream stream)
    {
        var rows = new List<IReadOnlyDictionary<string, string>>();
        await foreach (var row in PriceFileReader.ReadRowsAsync(stream))
            rows.Add(row);
        return rows;
    }

    [Fact]
    public void TestReadRowsVoiceIn()
    {
        using var stream = Fixture("voice_in_prices.csv.gz");

        var rows = PriceFileReader.ReadRows(stream).ToList();

        rows.Should().HaveCount(4);
        rows[0].Should().Equal(new Dictionary<string, string>
        {
            ["Country"] = "United Kingdom",
            ["Src country name"] = "",
            ["Source type"] = "cellular",
            ["Destination"] = "44800",
            ["Rate (USD, excl. VAT)"] = "",
            ["Connection fee (USD, excl. VAT)"] = "",
            ["Initial interval"] = "60",
            ["Next interval"] = "60",
            ["Reject calls"] = "true"
        });
        rows.Select(row => row["Destination"]).Should().Equal("44800", "4480012", "1800", "1800");
    }

    [Fact]
    public async Task TestReadRowsAsyncVoiceOut()
    {
        using var stream = Fixture("voice_out_prices.csv.gz");

        var rows = await ReadAllAsync(stream);

        rows.Should().HaveCount(4);
        rows[0].Should().Equal(new Dictionary<string, string>
        {
            ["Category"] = "International",
            ["Country"] = "Germany",
            ["Network"] = "Fixed",
            ["Prefix"] = "4930",
            ["Rate"] = "0.0045",
            ["Connection fee"] = "0.0",
            ["Initial Increment"] = "1",
            ["Recurring Increment"] = "1",
            ["Reject calls"] = "false",
            ["Next rate"] = "0.005",
            ["Next connection fee"] = "0.0",
            ["Next initial increment"] = "30",
            ["Next recurring increment"] = "6",
            ["Effective date"] = "2026-11-01T00:00:00Z",
            ["Comment"] = "Increase and Billing increment change"
        });
        rows[1]["Category"].Should().Be("Origin based");
        rows[2]["Prefix"].Should().Be("99950,99951,99952");
        rows[3]["Network"].Should().Be("New Breakout");
        rows[3]["Rate"].Should().Be("");
        rows[3]["Reject calls"].Should().Be("true");
    }

    [Fact]
    public void TestReadRowsPropagatesConsumerException()
    {
        using var stream = Fixture("voice_in_prices.csv.gz");
        var stop = new InvalidOperationException("stop");
        var calls = 0;

        var act = () =>
        {
            foreach (var _ in PriceFileReader.ReadRows(stream))
            {
                calls++;
                throw stop;
            }
        };

        act.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(stop);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task TestReadRowsRejectsUncompressedInput()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Country,Destination\nGermany,49\n"));

        var act = () => PriceFileReader.ReadRows(stream).ToList();
        var actAsync = () => ReadAllAsync(new MemoryStream(Encoding.UTF8.GetBytes("Country,Destination\nGermany,49\n")));

        act.Should().Throw<InvalidDataException>();
        await actAsync.Should().ThrowAsync<InvalidDataException>();
    }

    [Theory]
    [InlineData("voice_in_prices.csv.gz", 1)]
    [InlineData("voice_out_prices.csv.gz", 1)]
    [InlineData("voice_in_prices.csv.gz", 2)]
    [InlineData("voice_in_prices.csv.gz", 0)]
    public async Task TestReadRowsRejectsTruncatedGzip(string fixture, int cut)
    {
        var bytes = PricesServer.LoadBytes("prices/" + fixture);
        var truncated = cut switch
        {
            0 => Array.Empty<byte>(),
            1 => bytes[..^1],
            _ => bytes[..(bytes.Length / cut)]
        };

        var act = () => PriceFileReader.ReadRows(new MemoryStream(truncated)).ToList();
        var actAsync = () => ReadAllAsync(new MemoryStream(truncated));

        act.Should().Throw<InvalidDataException>().Which.Message.Should().Be("Price file: unexpected end of gzip data");
        (await actAsync.Should().ThrowAsync<InvalidDataException>()).Which.Message
            .Should().Be("Price file: unexpected end of gzip data");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(8192)]
    public async Task TestReadRowsAcceptsShortReads(int maxRead)
    {
        var lines = Enumerable.Range(0, 5000).Select(i => $"{i},0.{i:D6}");
        var gzip = Gzip("Prefix,Rate\n" + string.Join("\n", lines) + "\n").ToArray();

        var rows = PriceFileReader.ReadRows(new ChunkedStream(gzip, maxRead)).ToList();
        var asyncRows = await ReadAllAsync(new ChunkedStream(gzip, maxRead));

        rows.Should().HaveCount(5000);
        asyncRows.Should().HaveCount(5000);
        rows[^1]["Rate"].Should().Be("0.004999");
    }

    // Returns at most maxRead bytes per read, as a file or network stream may.
    private sealed class ChunkedStream(byte[] data, int maxRead) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, maxRead));

        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, maxRead)]);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, maxRead)], cancellationToken);
    }

    [Theory]
    [InlineData("voice_in_prices.csv.gz")]
    [InlineData("voice_out_prices.csv.gz")]
    public async Task TestReadRowsReadsIntactGzipToTheEnd(string fixture)
    {
        using var stream = Fixture(fixture);

        var rows = PriceFileReader.ReadRows(stream).ToList();
        stream.Position = 0;
        var asyncRows = await ReadAllAsync(stream);

        rows.Should().HaveCount(4);
        asyncRows.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("﻿Country,Rate\nUS,0.1\n")]
    [InlineData("﻿\"Country\",Rate\nUS,0.1\n")]
    public async Task TestReadRowsSkipsByteOrderMark(string csv)
    {
        var rows = PriceFileReader.ReadRows(Gzip(csv)).ToList();
        var asyncRows = await ReadAllAsync(Gzip(csv));

        rows.Should().ContainSingle().Which["Country"].Should().Be("US");
        asyncRows.Should().ContainSingle().Which["Country"].Should().Be("US");
    }

    [Fact]
    public void TestReadRowsQuotedFields()
    {
        const string csv = "\"Prefix\",\"Rate (USD, excl. VAT)\",Comment\r\n" +
                           "\"99950,99951\",0.25,\"say \"\"hi\"\"\"\r\n" +
                           "\r\n" +
                           "49,,\"two\r\nlines\"\r\n" +
                           " 1 ,\"\",last";

        var rows = PriceFileReader.ReadRows(Gzip(csv)).ToList();

        rows.Should().HaveCount(3);
        rows[0].Should().Equal(new Dictionary<string, string>
        {
            ["Prefix"] = "99950,99951",
            ["Rate (USD, excl. VAT)"] = "0.25",
            ["Comment"] = "say \"hi\""
        });
        rows[1]["Comment"].Should().Be("two\nlines");
        rows[1]["Rate (USD, excl. VAT)"].Should().Be("");
        rows[2]["Prefix"].Should().Be(" 1 ", "spaces are kept");
        rows[2]["Comment"].Should().Be("last", "the last line needs no line end");
    }

    [Theory]
    [InlineData("A,B\n1,2,3\n", "line 2: wrong number of fields")]
    [InlineData("A,B\n1\n", "line 2: wrong number of fields")]
    [InlineData("A,B\n1,x\"y\n", "bare \"")]
    [InlineData("A,B\n1,\"x\"y\n", "extraneous or missing \"")]
    [InlineData("A,B\n1,\"open\n", "extraneous or missing \"")]
    public async Task TestReadRowsRejectsMalformedCsv(string csv, string message)
    {
        var act = () => PriceFileReader.ReadRows(Gzip(csv)).ToList();
        var actAsync = () => ReadAllAsync(Gzip(csv));

        act.Should().Throw<InvalidDataException>().Which.Message.Should().Contain(message);
        (await actAsync.Should().ThrowAsync<InvalidDataException>()).Which.Message.Should().Contain(message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Country,Rate\n")]
    public void TestReadRowsWithoutRows(string csv)
    {
        PriceFileReader.ReadRows(Gzip(csv)).Should().BeEmpty();
    }

    [Fact]
    public void TestReadRowsLeavesStreamOpen()
    {
        using var stream = Fixture("voice_in_prices.csv.gz");

        PriceFileReader.ReadRows(stream).ToList();

        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task TestReadRowsAsyncHonoursCancellation()
    {
        using var stream = Fixture("voice_in_prices.csv.gz");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () =>
        {
            await foreach (var _ in PriceFileReader.ReadRowsAsync(stream, cts.Token))
            {
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
