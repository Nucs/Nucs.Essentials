using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nucs.Csvs {
    [ComVisible(true)]
    [Serializable]
    public class MemoryReader : TextReader {
        private int _pos;
        private int _length;
        private ReadOnlyMemory<char> _memory;
        private bool _disposed;

        public void Reuse(string s) {
            if (s == null) throw new ArgumentNullException(nameof(s));
            Reuse(s.AsMemory());
        }

        public void Reuse(ReadOnlyMemory<char> s) {
            _memory = s;
            _length = s.Length;
            _pos = 0;
            _disposed = false;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.IO.AdvStringReader" /> class that reads from the specified string.</summary>
        /// <param name="s">The string to which the <see cref="T:System.IO.AdvStringReader" /> should be initialized.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="s" /> parameter is <see langword="null" />.</exception>
        public MemoryReader() : this(string.Empty) { }

        /// <summary>Initializes a new instance of the <see cref="T:System.IO.AdvStringReader" /> class that reads from the specified string.</summary>
        /// <param name="s">The string to which the <see cref="T:System.IO.AdvStringReader" /> should be initialized.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="s" /> parameter is <see langword="null" />.</exception>
        public MemoryReader(string s) {
            if (s == null) throw new ArgumentNullException(nameof(s));
            _memory = s.AsMemory();
            _length = s.Length;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.IO.AdvStringReader" /> class that reads from the specified string.</summary>
        /// <param name="s">The string to which the <see cref="T:System.IO.AdvStringReader" /> should be initialized.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="s" /> parameter is <see langword="null" />.</exception>
        public MemoryReader(ReadOnlyMemory<char> s) {
            _memory = s;
            _length = s.Length;
        }

        /// <summary>Closes the <see cref="T:System.IO.AdvStringReader" />.</summary>
        public override void Close() =>
            Dispose(true);

        /// <summary>Releases the unmanaged resources used by the <see cref="T:System.IO.AdvStringReader" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing) {
            _memory = default;
            _pos = 0;
            _length = 0;
            _disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>Returns the next available character but does not consume it.</summary>
        /// <returns>An integer representing the next character to be read, or -1 if no more characters are available or the stream does not support seeking.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current reader is closed.</exception>
        public override int Peek() {
            if (_disposed)
                throw new ObjectDisposedException((string) null, "Environment.GetResourceString(\"ObjectDisposed_WriterClosed\")");
            return _pos == _length ? -1 : (int) _memory.Span[_pos];
        }

        /// <summary>Reads the next character from the input string and advances the character position by one character.</summary>
        /// <returns>The next character from the underlying string, or -1 if no more characters are available.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current reader is closed.</exception>
        public override int Read() {
            if (_disposed)
                throw new ObjectDisposedException((string) null, "Environment.GetResourceString(\"ObjectDisposed_WriterClosed\")");
            return _pos == _length ? -1 : (int) _memory.Span[_pos++];
        }

        /// <summary>Reads a block of characters from the input string and advances the character position by <paramref name="count" />.</summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between <paramref name="index" /> and (<paramref name="index" /> + <paramref name="count" /> - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The starting index in the buffer.</param>
        /// <param name="count">The number of characters to read.</param>
        /// <returns>The total number of characters read into the buffer. This can be less than the number of characters requested if that many characters are not currently available, or zero if the end of the underlying string has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current reader is closed.</exception>
        public override int Read([In, Out] char[] buffer, int index, int count) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Environment.GetResourceString(\"ArgumentNull_Buffer\")");
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
            if (buffer.Length - index < count)
                throw new ArgumentException("Environment.GetResourceString(\"Argument_InvalidOffLen\")");
            if (_disposed)
                throw new ObjectDisposedException((string) null, "Environment.GetResourceString(\"ObjectDisposed_WriterClosed\")");

            int count1 = _length - _pos;
            if (count1 > 0) {
                if (count1 > count)
                    count1 = count;

                _memory.Span.Slice(_pos, count1).CopyTo(buffer.AsSpan(index, count1));
                _pos += count1;
            }

            return count1;
        }

        /// <summary>Reads all characters from the current position to the end of the string and returns them as a single string.</summary>
        /// <returns>The content from the current position to the end of the underlying string.</returns>
        /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The current reader is closed.</exception>
        public override string ReadToEnd() {
            if (_disposed)
                throw new ObjectDisposedException((string) null, "Environment.GetResourceString(\"ObjectDisposed_WriterClosed\")");
            string str = _pos != 0 ? _memory.Slice(_pos, _length - _pos).ToString() : _memory.ToString();
            _pos = _length;
            return str;
        }

        /// <summary>Reads a line of characters from the current string and returns the data as a string.</summary>
        /// <returns>The next line from the current string, or <see langword="null" /> if the end of the string is reached.</returns>
        /// <exception cref="T:System.ObjectDisposedException">The current reader is closed.</exception>
        /// <exception cref="T:System.OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string.</exception>
        public override string ReadLine() {
            if (_disposed)
                throw new ObjectDisposedException((string) null, "Environment.GetResourceString(\"ObjectDisposed_WriterClosed\")");
            var mem = _memory.Span;
            int pos;
            for (pos = _pos; pos < _length; ++pos) {
                char ch = mem[pos];
                switch (ch) {
                    case '\n':
                    case '\r':
                        ReadOnlySpan<char> str = mem.Slice(_pos, pos - _pos);
                        _pos = pos + 1;
                        if (ch == '\r' && _pos < _length && mem[_pos] == '\n')
                            ++_pos;
                        return str.ToString();
                    default:
                        continue;
                }
            }

            if (pos <= _pos)
                return (string) null;
            string str1 = _memory.Slice(_pos, pos - _pos).ToString();
            _pos = pos;
            return str1;
        }

        /// <summary>Reads a line of characters asynchronously from the current string and returns the data as a string.</summary>
        /// <returns>A task that represents the asynchronous read operation. The value of the <paramref name="TResult" /> parameter contains the next line from the string reader, or is <see langword="null" /> if all the characters have been read.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The number of characters in the next line is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The string reader has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        [ComVisible(false)]
        public override Task<string> ReadLineAsync() =>
            Task.FromResult<string>(ReadLine());

        /// <summary>Reads all characters from the current position to the end of the string asynchronously and returns them as a single string.</summary>
        /// <returns>A task that represents the asynchronous read operation. The value of the <paramref name="TResult" /> parameter contains a string with the characters from the current position to the end of the string.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The number of characters is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The string reader has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        [ComVisible(false)]
        public override Task<string> ReadToEndAsync() =>
            Task.FromResult<string>(ReadToEnd());

        /// <summary>Reads a specified maximum number of characters from the current string asynchronously and writes the data to a buffer, beginning at the specified index.</summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between <paramref name="index" /> and (<paramref name="index" /> + <paramref name="count" /> - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The position in <paramref name="buffer" /> at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read. If the end of the string is reached before the specified number of characters is written into the buffer, the method returns.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <paramref name="TResult" /> parameter contains the total number of bytes read into the buffer. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the string has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The sum of <paramref name="index" /> and <paramref name="count" /> is larger than the buffer length.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The string reader has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        [ComVisible(false)]
        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Environment.GetResourceString(\"ArgumentNull_Buffer\")");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
            if (buffer.Length - index < count)
                throw new ArgumentException("Environment.GetResourceString(\"Argument_InvalidOffLen\")");
            return Task.FromResult<int>(ReadBlock(buffer, index, count));
        }

        /// <summary>Reads a specified maximum number of characters from the current string asynchronously and writes the data to a buffer, beginning at the specified index.</summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between <paramref name="index" /> and (<paramref name="index" /> + <paramref name="count" /> - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The position in <paramref name="buffer" /> at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read. If the end of the string is reached before the specified number of characters is written into the buffer, the method returns.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the <paramref name="TResult" /> parameter contains the total number of bytes read into the buffer. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the string has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.</exception>
        /// <exception cref="T:System.ArgumentException">The sum of <paramref name="index" /> and <paramref name="count" /> is larger than the buffer length.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The string reader has been disposed.</exception>
        /// <exception cref="T:System.InvalidOperationException">The reader is currently in use by a previous read operation.</exception>
        [ComVisible(false)]
        public override Task<int> ReadAsync(char[] buffer, int index, int count) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Environment.GetResourceString(\"ArgumentNull_Buffer\")");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? nameof(index) : nameof(count), "Environment.GetResourceString(\"ArgumentOutOfRange_NeedNonNegNum\")");
            if (buffer.Length - index < count)
                throw new ArgumentException("Environment.GetResourceString(\"Argument_InvalidOffLen\")");
            return Task.FromResult<int>(Read(buffer, index, count));
        }
    }
}