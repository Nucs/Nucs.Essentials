using System;
using System.Runtime.InteropServices;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split by a delimiter without copying
    /// </summary>
    public ref struct MemoryLineReader {
        private readonly ReadOnlyMemory<char> _line;
        private int _lastIndex;

        public MemoryLineReader(ReadOnlyMemory<char> span) {
            _line = span;
            _lastIndex = 0;
        }

        public MemoryLineReader(string text) {
            _line = text.AsMemory();
            _lastIndex = 0;
        }

        public bool HasNext => _lastIndex < _line.Length;

        public int PointerIndex {
            get => _lastIndex;
            set => _lastIndex = value;
        }

        /// <returns>Number of items in this row</returns>
        public readonly int CountItems(char delimiter = ',') {
            var line = _line.Span;
            if (line.IsEmpty)
                return 0;

            int items = 1;
            unsafe {
                var len = line.Length;
                fixed (char* str = line) {
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
            var line = _line.Span;
            if (line.IsEmpty)
                return 0;

            if (delimiter.Length == 1)
                return CountItems(delimiter[0]);

            int items = 1;
            unsafe {
                var len = line.Length;
                var del_len = delimiter.Length;
                fixed (char* str = line) {
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
            var line = _line.Span;
            if (_lastIndex >= line.Length)
                return ReadOnlySpan<char>.Empty;

            line = line.Slice(_lastIndex);
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
            var line = _line.Span;
            if (_lastIndex >= line.Length)
                return ReadOnlySpan<char>.Empty;

            line = line.Slice(_lastIndex);
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
            var line = _line.Span;
            for (int j = 0; j < delimiters; j++) {
                if (_lastIndex >= line.Length)
                    break;

                line = line.Slice(_lastIndex);
                var i = line.IndexOf(delimiter);
                if (i == -1) {
                    _lastIndex += line.Length;
                    break;
                }

                _lastIndex += i + 1;
            }
        }

        public void Skip(int delimiters, string delimiter) {
            var span = _line.Span;
            for (int j = 0; j < delimiters; j++) {
                if (_lastIndex >= span.Length)
                    break;

                var line = span.Slice(_lastIndex);
                var i = line.IndexOf(delimiter);
                if (i == -1) {
                    _lastIndex += line.Length;
                    break;
                }

                _lastIndex += i + delimiter.Length;
            }
        }

        public static implicit operator MemoryLineReader(ReadOnlyMemory<char> text) {
            return new MemoryLineReader(text);
        }
    }
}