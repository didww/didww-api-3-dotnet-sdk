using System.Text;

namespace Didww.Api3.Prices;

/// <summary>
/// An RFC 4180 record reader: comma-separated fields, double-quoted fields that
/// may hold commas, line breaks and doubled quotes; LF, CRLF or CR line ends (a
/// line break inside a quoted field reads as LF). Empty lines are skipped and spaces
/// are kept. A quote inside an unquoted field, text after a closing quote and
/// an unterminated quoted field raise <see cref="InvalidDataException"/>.
/// </summary>
internal sealed class CsvRecordReader
{
    private enum State
    {
        FieldStart,
        Unquoted,
        Quoted,
        QuoteInQuoted
    }

    private readonly TextReader _reader;
    private int _line;

    internal CsvRecordReader(TextReader reader)
    {
        _reader = reader;
    }

    /// <summary>The line on which the last record returned starts (1-based).</summary>
    internal int RecordLine { get; private set; }

    /// <summary>Reads the next record, or returns null at the end of the input.</summary>
    internal List<string>? Read()
    {
        string? line;
        do
        {
            line = _reader.ReadLine();
            if (line == null)
                return null;
            _line++;
        } while (line.Length == 0);

        var record = new RecordBuilder();
        RecordLine = _line;
        while (!record.ParseLine(line, _line))
        {
            line = _reader.ReadLine() ?? throw Error(RecordLine, "extraneous or missing \" in quoted-field");
            _line++;
        }
        return record.Fields;
    }

    /// <summary>Reads the next record, or returns null at the end of the input.</summary>
    internal async ValueTask<List<string>?> ReadAsync(CancellationToken cancellationToken)
    {
        string? line;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
                return null;
            _line++;
        } while (line.Length == 0);

        var record = new RecordBuilder();
        RecordLine = _line;
        while (!record.ParseLine(line, _line))
        {
            line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ??
                   throw Error(RecordLine, "extraneous or missing \" in quoted-field");
            _line++;
        }
        return record.Fields;
    }

    private static InvalidDataException Error(int line, string message) =>
        new($"Price file: parse error on line {line}: {message}");

    // The fields of one record, built line by line.
    private sealed class RecordBuilder
    {
        private readonly StringBuilder _field = new();
        private State _state = State.FieldStart;
        private bool _continued;

        internal List<string> Fields { get; } = new();

        // Parses one line; returns false while a quoted field continues on the next line.
        internal bool ParseLine(string line, int lineNumber)
        {
            if (_continued)
                _field.Append('\n');

            foreach (var c in line)
                Accept(c, lineNumber);

            if (_state == State.Quoted)
            {
                _continued = true;
                return false;
            }
            EndField();
            return true;
        }

        private void Accept(char c, int lineNumber)
        {
            switch (_state)
            {
                case State.FieldStart:
                    AcceptAtFieldStart(c, lineNumber);
                    break;
                case State.Unquoted:
                    AcceptUnquoted(c, lineNumber);
                    break;
                case State.Quoted:
                    AcceptQuoted(c);
                    break;
                case State.QuoteInQuoted:
                    AcceptQuoteInQuoted(c, lineNumber);
                    break;
            }
        }

        private void AcceptAtFieldStart(char c, int lineNumber)
        {
            if (c == '"')
            {
                _state = State.Quoted;
                return;
            }
            _state = State.Unquoted;
            AcceptUnquoted(c, lineNumber);
        }

        private void AcceptUnquoted(char c, int lineNumber)
        {
            if (c == ',')
                EndField();
            else if (c == '"')
                throw Error(lineNumber, "bare \" in non-quoted field");
            else
                _field.Append(c);
        }

        private void AcceptQuoted(char c)
        {
            if (c == '"')
                _state = State.QuoteInQuoted;
            else
                _field.Append(c);
        }

        private void AcceptQuoteInQuoted(char c, int lineNumber)
        {
            if (c == '"')
            {
                _field.Append('"');
                _state = State.Quoted;
            }
            else if (c == ',')
            {
                EndField();
            }
            else
            {
                throw Error(lineNumber, "extraneous or missing \" in quoted-field");
            }
        }

        private void EndField()
        {
            Fields.Add(_field.ToString());
            _field.Clear();
            _state = State.FieldStart;
        }
    }
}
