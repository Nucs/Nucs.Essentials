using System;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split by a delimiter without copying
    /// </summary>
    public ref struct LineReader {
        private readonly ReadOnlySpan<char> _line;
        private int _lastIndex;

        public LineReader(ReadOnlySpan<char> span) {
            _line = span;
            _lastIndex = 0;
        }

        public bool HasNext => _lastIndex < _line.Length;

        public int PointerIndex {
            get => _lastIndex;
            set => _lastIndex = value;
        }

        public LineReader(string text) {
            _line = text;
            _lastIndex = 0;
        }

        /// <returns>Number of items in this row</returns>
        public readonly int CountItems(char delimiter = ',') {
            if (_line.IsEmpty)
                return 0;

            int items = 1;
            unsafe {
                var len = _line.Length;
                fixed (char* str = _line) {
                    for (int i = 0; i < len; i++) {
                        if (str[i] == ',')
                            items++;
                    }
                }
            }

            return items;
        }

        public void Reset() {
            _lastIndex = 0;
        }

        public ReadOnlySpan<char> Next(char delimiter = ',') {
            if (_lastIndex >= _line.Length)
                return ReadOnlySpan<char>.Empty;

            var line = _line.Slice(_lastIndex);
            var i = line.IndexOf(delimiter);
            if (i == -1) {
                _lastIndex += line.Length;
                return line;
            }

            var newSlice = line.Slice(0, i);
            _lastIndex += i + 1;
            return newSlice;
        }

        public void Skip(int delimiters, char delimiter = ',') {
            for (int j = 0; j < delimiters; j++) {
                if (_lastIndex >= _line.Length)
                    break;

                var line = _line.Slice(_lastIndex);
                var i = line.IndexOf(delimiter);
                if (i == -1) {
                    _lastIndex += line.Length;
                    break;
                }

                _lastIndex += i + 1;
            }

        }

        public static implicit operator LineReader(ReadOnlySpan<char> text) {
            return new LineReader(text);
        }
    }
}