using System;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split based on new line characters that's also cross platform and resolved during construction or passed as a parameter.
    /// </summary>
    public ref struct RowReader {

        private readonly ReadOnlySpan<char> _line;
        private int _lastIndex;

        private readonly ReadOnlySpan<char> _delimiter;

        /// <summary>
        ///     The delimiter resolved during construction
        /// </summary>
        public LineDelimiter ResolvedDelimiter => _delimiter.Length == 2 ? LineDelimiter.CRLF : _delimiter[0] == '\r' ? LineDelimiter.CR : LineDelimiter.LF;


        public RowReader(ReadOnlySpan<char> span) {
            _line = span;
            _lastIndex = 0;

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
            _lastIndex = 0;
            //identify delimiter type
            _delimiter = delimiter.AsString();
        }

        public RowReader(string text) : this((ReadOnlySpan<char>) text) { }

        public bool HasNext => _lastIndex < _line.Length;

        public int PointerIndex {
            get => _lastIndex;
            set => _lastIndex = value;
        }

        public void Reset() {
            _lastIndex = 0;
        }


        public ReadOnlySpan<char> Next() {
            if (_lastIndex >= _line.Length)
                return ReadOnlySpan<char>.Empty;

            var line = _line.Slice(_lastIndex);
            var i = line.IndexOf(_delimiter);
            if (i == -1) {
                _lastIndex += line.Length;
                return line;
            }

            var newSlice = line.Slice(0, i);
            _lastIndex += i + _delimiter.Length;
            return newSlice;
        }

        public void Skip(int rows) {
            for (int j = 0; j < rows; j++) {
                if (_lastIndex >= _line.Length)
                    break;

                var line = _line.Slice(_lastIndex);
                var i = line.IndexOf(_delimiter);
                if (i == -1) {
                    _lastIndex += line.Length;
                    break;
                }

                _lastIndex += i + _delimiter.Length;
            }
        }

        public static implicit operator RowReader(ReadOnlySpan<char> text) {
            return new RowReader(text);
        }
    }
}