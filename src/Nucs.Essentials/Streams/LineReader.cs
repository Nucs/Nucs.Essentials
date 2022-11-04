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

        public LineReader(string text) {
            _line = text;
            _lastIndex = 0;
        }

        public bool HasNext => _lastIndex < _line.Length;

        public int PointerIndex {
            get => _lastIndex;
            set => _lastIndex = value;
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
                        if (str[i] == delimiter)
                            items++;
                    }
                }
            }

            return items;
        }

        /// <returns>Number of items in this row</returns>
        public readonly int CountItems(string delimiter) {
            if (_line.IsEmpty)
                return 0;

            if (delimiter.Length == 1)
                return CountItems(delimiter[0]);

            int items = 1;
            unsafe {
                var len = _line.Length;
                var del_len = delimiter.Length;
                fixed (char* str = _line) {
                    fixed (char* del = delimiter) {
                        for (int i = 0; i < len; i++) {
                            for (int j = 0; j < del_len; j++) {
                                if (str[i + j] != del[j])
                                    goto nextItem;
                            }

                            items++; //found
                            nextItem: ;
                        }
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

        public ReadOnlySpan<char> Next(string delimiter) {
            if (_lastIndex >= _line.Length)
                return ReadOnlySpan<char>.Empty;

            var line = _line.Slice(_lastIndex);
            var i = line.IndexOf(delimiter);
            if (i == -1) {
                _lastIndex += line.Length;
                return line;
            }

            var newSlice = line.Slice(0, i);
            _lastIndex += i + delimiter.Length;
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

        public void Skip(int delimiters, string delimiter) {
            for (int j = 0; j < delimiters; j++) {
                if (_lastIndex >= _line.Length)
                    break;

                var line = _line.Slice(_lastIndex);
                var i = line.IndexOf(delimiter);
                if (i == -1) {
                    _lastIndex += line.Length;
                    break;
                }

                _lastIndex += i + delimiter.Length;
            }
        }

        public static implicit operator LineReader(ReadOnlySpan<char> text) {
            return new LineReader(text);
        }
    }
}