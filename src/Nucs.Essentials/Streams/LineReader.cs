using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Text;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split by a delimiter without copying
    /// </summary>
    public ref struct LineReader {
        private ReadOnlySpan<char> _line;

        public LineReader(ReadOnlySpan<char> span) {
            _line = span;
        }

        public LineReader(string text) {
            _line = text;
        }

        public LineReader(ReadOnlyMemory<char> text) {
            _line = text.Span;
        }

        public bool HasNext => _line.Length != 0;

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
            var line = _line;
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

        public ReadOnlySpan<char> Next(char delimiter = ',') {
            ref var line = ref MemoryMarshal.GetReference(_line);
            var length = _line.Length;
            if (length == 0)
                return default;

            var nextDelimiterIndex = SpanStringHelper.IndexOf(ref line, delimiter, length);
            if (nextDelimiterIndex == -1) {
                _line = default;
                return MemoryMarshal.CreateReadOnlySpan(ref line, length);
            }

            _line = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref line, (nint) nextDelimiterIndex + 1), length - nextDelimiterIndex - 1);
            return MemoryMarshal.CreateReadOnlySpan(ref line, nextDelimiterIndex);
        }

        public unsafe ReadOnlySpan<char> Next(string delimiter) {
            var length = _line.Length;
            if (length == 0)
                return default;
            
            ref var line = ref MemoryMarshal.GetReference(_line);
            var delLength = delimiter.Length;
            int nextDelimiterIndex = SpanStringHelper.IndexOf(ref line, length, ref Unsafe.AsRef(delimiter.GetPinnableReference()), delLength);
            if (nextDelimiterIndex == -1) {
                _line = default;
                return MemoryMarshal.CreateReadOnlySpan(ref line, length);
            }

            _line = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref line, (nint) nextDelimiterIndex + delLength), length - nextDelimiterIndex - delLength);
            return MemoryMarshal.CreateReadOnlySpan(ref line, nextDelimiterIndex);
        }

        public ReadOnlySpan<char> NextTillEnd() {
            var line = _line;
            _line = default;
            return _line;
        }

        public void Skip(int delimiters, char delimiter = ',') {
            var length = _line.Length;
            if (length == 0)
                return;
            
            ref var line = ref MemoryMarshal.GetReference(_line);
            for (int j = delimiters - 1; j >= 0 && length > 0; j--) {
                var i = SpanStringHelper.IndexOf(ref line, delimiter, length);
                if (i == -1) {
                    _line = default;
                    return;
                }

                line = ref Unsafe.Add(ref line, (nint) i + 1);
                length -= i + 1;
            }

            _line = length == 0 ? default : MemoryMarshal.CreateReadOnlySpan(ref line, length);
        }

        public void Skip(int delimiters, string delimiter) {
            var length = _line.Length;
            if (length == 0)
                return;

            ref var line = ref MemoryMarshal.GetReference(_line);
            var delLength = delimiter.Length;
            ref var del = ref Unsafe.AsRef(delimiter.GetPinnableReference());
            for (int j = delimiters - 1; j >= 0 && length > 0; j--) {
                var i = SpanStringHelper.IndexOf(ref line, length, ref del, delLength);
                if (i == -1) {
                    _line = default;
                    return;
                }

                line = ref Unsafe.Add(ref line, (nint) i + delLength);
                length -= i + delLength;
            }

            _line = length == 0 ? default : MemoryMarshal.CreateReadOnlySpan(ref line, length);
        }

        public static implicit operator LineReader(ReadOnlySpan<char> text) {
            return new LineReader(text);
        }
    }
}