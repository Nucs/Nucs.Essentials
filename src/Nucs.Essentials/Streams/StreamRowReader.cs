using System;
using System.Buffers;
using System.IO;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Nucs.Collections.Layouts;
using Nucs.Csvs;
using Nucs.Text;

namespace Nucs.Extensions {
    /// <summary>
    ///     Performantly does a string split based on new line characters that's also cross platform and resolved during construction or passed as a parameter.
    /// </summary>
    public ref struct StreamRowReader {
        public static Encoding Encoding = Encoding.UTF8;

        private readonly Stream _stream;
        private readonly Span<byte> _buffer;
        private readonly StringSplitOptions _splitOptions;
        private ValueStringBuilder _sb;
        private int _readSoFar;
        private readonly byte[]? _arrayToReturnToPool;
        private int _averageRowLength;

        private readonly ReadOnlySpan<char> _delimiter;

        /// <summary>
        ///     The delimiter resolved during construction
        /// </summary>
        public LineDelimiter Delimiter => _delimiter.Length == 2 ? LineDelimiter.CRLF : _delimiter[0] == '\r' ? LineDelimiter.CR : LineDelimiter.LF;

        public StreamRowReader(Stream stream, LineDelimiter delimiter, int bufferSize = 16384, int rowBufferSize = 256, StringSplitOptions splitOptions = StringSplitOptions.None)
            : this(stream, delimiter, Span<byte>.Empty, rowBufferSize, splitOptions) {
            _arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, 32));
            _buffer = _arrayToReturnToPool;
        }

        public StreamRowReader(Stream stream, LineDelimiter delimiter, byte[] buffer, int rowBufferSize = 256, StringSplitOptions splitOptions = StringSplitOptions.None)
            : this(stream, delimiter, (Span<byte>) buffer, rowBufferSize, splitOptions) { }

        public StreamRowReader(Stream stream, LineDelimiter delimiter, Span<byte> buffer, int rowBufferSize = 256, StringSplitOptions splitOptions = StringSplitOptions.None) {
            _stream = stream;
            _delimiter = delimiter switch {
                LineDelimiter.CR   => "\r",
                LineDelimiter.LF   => "\n",
                LineDelimiter.CRLF => "\r\n",
                _                  => throw new ArgumentOutOfRangeException(nameof(delimiter), delimiter, null)
            };
            _sb = new ValueStringBuilder(Math.Max(rowBufferSize, 32));
            _buffer = buffer;
            _splitOptions = splitOptions;
            _arrayToReturnToPool = default;
            _readSoFar = 0;
            _averageRowLength = (int) (_sb.Capacity * 0.8);
        }

        public bool HasNext {
            get {
                if (_sb.Length - _readSoFar > 0)
                    return true;

                if (!_stream.CanRead) return false;
                try {
                    return _stream.Position < _stream.Length;
                } catch (NotSupportedException e) {
                    return false;
                } catch (NotImplementedException e) {
                    return false;
                }
            }
        }

        public void Reset() {
            _sb.Clear();
            try {
                _stream.Position = 0;
            } catch (NotSupportedException e) { } catch (NotImplementedException e) { }

            _readSoFar = 0;
        }

        private int multiplier = 0;


        public ReadOnlySpan<char> Next() {
            _lookupNext:
            int readSoFar = _readSoFar;
            if (readSoFar > multiplier /*buffer should fit 254/256 messages*/) {
                _sb.RemoveStart(readSoFar);
                _readSoFar = 0;
                readSoFar = 0;
            }

            //try reading from already read chars
            int i = -1;
            var readable = _sb.AsSpan(readSoFar);
            if (!readable.IsEmpty && (i = readable.IndexOf(_delimiter)) != -1)
                goto _returnSlice;

            //we have to process the stream to find end of line.
            do {
                var streamRead = _stream.Read(_buffer);
                if (streamRead > 0)
                    _sb.Append(_buffer.Slice(0, streamRead), Encoding);

                readable = _sb.AsSpan(readSoFar); //get me everything that's readable
                i = readable.IndexOf(_delimiter);
                if (streamRead == 0 && i == -1) {
                    //was faulty, we have to return the whole thing.
                    var remainder = _sb.Length - readSoFar;
                    _readSoFar += remainder;
                    return _sb.AsSpan(readSoFar, remainder);
                }
            } while (i == -1); //keep reading until we find the delimiter

            _returnSlice:
            _readSoFar += i + _delimiter.Length;
            var newSlice = readable.Slice(0, i);
            
            //handle 
            multiplier = multiplier == 0 ? (i * 256) : (multiplier + i * 256) / 2;
            if (multiplier > _sb.Capacity)
                _sb.EnsureCapacity(multiplier);

            //handle split options
            if (_splitOptions == StringSplitOptions.None)
                return newSlice;

            if (_splitOptions.HasFlag(StringSplitOptions.TrimEntries) && !newSlice.IsEmpty)
                newSlice = newSlice.Trim();

            if (_splitOptions.HasFlag(StringSplitOptions.RemoveEmptyEntries) && newSlice.IsEmpty)
                goto _lookupNext; //skip empty lines

            return newSlice;
        }

        public void Skip(int rows) {
            for (int i = 0; i < rows; i++) {
                //has to be called via Next to handle split options
                Next();
            }
        }

        public void Dispose() {
            var loanedArray = _arrayToReturnToPool;
            _sb.Dispose();
            this = default;
            if (loanedArray != null) {
                ArrayPool<byte>.Shared.Return(loanedArray);
            }
        }
    }
}