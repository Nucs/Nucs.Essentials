using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Text;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split based on new line characters that's also cross platform and resolved during construction or passed as a parameter.
    /// </summary>
    public ref struct RowReader {
        private ReadOnlySpan<char> _line;
        private readonly ReadOnlySpan<char> _delimiter;

        /// <summary>
        ///     The delimiter resolved during construction
        /// </summary>
        public LineDelimiter ResolvedDelimiter => _delimiter.Length == 2 ? LineDelimiter.CRLF : _delimiter[0] == '\r' ? LineDelimiter.CR : LineDelimiter.LF;

        public RowReader(ReadOnlyMemory<char> memory) : this(memory.Span) { }
        public RowReader(ReadOnlyMemory<char> memory, LineDelimiter delimiter) : this(memory.Span, delimiter) { }

        public RowReader(ReadOnlySpan<char> span) {
            _line = span;

            //identify delimiter type
            int len = span.Length;
            for (int i = 0; i < len; i++) {
                if (span[i] == '\r') {
                    if (i + 1 < len && span[i + 1] == '\n') {
                        _delimiter = "\r\n";
                        return;
                    }

                    _delimiter = "\r";
                    return;
                }

                if (span[i] == '\n') {
                    _delimiter = "\n";
                    return;
                }
            }

            //shouldn't get here, if does - set default.
            throw new ArgumentException($"Unable to resolve the delimiter type. The span is not delimited by any of the supported delimiters: \\r, \\n, \\r\\n");
        }

        public RowReader(ReadOnlySpan<char> span, LineDelimiter delimiter) {
            _line = span;
            //identify delimiter type
            _delimiter = delimiter.AsString();
        }

        public RowReader(string text) : this((ReadOnlySpan<char>) text) { }
        public RowReader(string text, LineDelimiter delimiter) : this((ReadOnlySpan<char>) text, delimiter) { }

        public bool HasNext => _line.Length != 0;

        public ReadOnlySpan<char> Next() {
            var length = _line.Length;
            if (length == 0)
                return default;

            ref var line = ref MemoryMarshal.GetReference(_line);
            var delLength = _delimiter.Length;
            var nextDelimiterIndex = delLength == 1
                ? SpanStringHelper.IndexOf(ref line, MemoryMarshal.GetReference(_delimiter), length)
                : SpanStringHelper.IndexOf(ref line, length, ref MemoryMarshal.GetReference(_delimiter), delLength);

            if (nextDelimiterIndex == -1) {
                _line = default;
                return MemoryMarshal.CreateReadOnlySpan(ref line, length);
            }

            _line = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref line, (nint) nextDelimiterIndex + delLength), length - nextDelimiterIndex - delLength);
            return MemoryMarshal.CreateReadOnlySpan(ref line, nextDelimiterIndex);
        }

        public void Skip(int rows) {
            var length = _line.Length;
            if (length == 0)
                return;

            ref var del = ref MemoryMarshal.GetReference(_delimiter);
            ref var line = ref MemoryMarshal.GetReference(_line);
            var delLength = _delimiter.Length;
            for (int j = rows - 1; j >= 0 && length > 0; j--) {
                var i = delLength == 1
                    ? SpanStringHelper.IndexOf(ref line, del, length)
                    : SpanStringHelper.IndexOf(ref line, length, ref del, delLength);
                if (i == -1) {
                    _line = default;
                    return;
                }

                line = ref Unsafe.Add(ref line, (nint) i + delLength);
                length -= i + delLength;
            }

            _line = length == 0 ? default : MemoryMarshal.CreateReadOnlySpan(ref line, length);
        }

        public static implicit operator RowReader(ReadOnlySpan<char> text) {
            return new RowReader(text);
        }

        public static implicit operator LineReader(RowReader text) {
            return new LineReader(text._line);
        }
    }
}