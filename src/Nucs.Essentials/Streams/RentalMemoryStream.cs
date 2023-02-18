using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Nucs.Streams {
    // A RentalMemoryStream represents a Stream in memory (ie, it has no backing store).
    // This stream may reduce the need for temporary buffers and files in 
    // an application.  
    // 
    // There are two ways to create a RentalMemoryStream.  You can initialize one
    // from an unsigned byte array, or you can create an empty one.  Empty 
    // memory streams are resizable, while ones created with a byte array provide
    // a stream "view" of the data.
    public sealed class RentalMemoryStream : Stream {
        private const int MaxByteArrayLength = 0X7FEFFFFF;
        private byte[] _buffer; // Either allocated internally or externally.
        private int _position; // read/write head.
        private int _length; // Number of bytes within the memory stream

        private int _capacity; // length of usable portion of buffer for stream
        // Note that _capacity == _buffer.Length for non-user-provided byte[]'s

        private bool _writable; // Can user write to this stream?
        private bool _isOpen; // Is this stream open or closed?

        private Task<int>? _lastReadTask; // The last successful task returned from ReadAsync

        private const int MemStreamMaxLength = int.MaxValue;

        public RentalMemoryStream()
            : this(0) { }

        public RentalMemoryStream(int capacity) {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be less than zero.");

            if (capacity != 0) {
                _buffer = ArrayPool<byte>.Shared.Rent(capacity);
                _capacity = _buffer.Length;
            } else
                _buffer = Array.Empty<byte>();

            _writable = true;
            _isOpen = true;
        }

        public override bool CanRead => _isOpen;

        public override bool CanSeek => _isOpen;

        public override bool CanWrite => _writable;

        private void EnsureNotClosed() {
            if (!_isOpen)
                throw new ObjectDisposedException("Stream is closed");
        }

        private void EnsureWriteable() {
            if (!CanWrite)
                throw new NotSupportedException("Stream is not writable");
        }

        protected override void Dispose(bool disposing) {
            try {
                if (disposing) {
                    _isOpen = false;
                    _writable = false;
                    // Don't set buffer to null - allow TryGetBuffer, GetBuffer & ToArray to work.
                    _lastReadTask = null;
                    var buffer = _buffer;
                    _buffer = Array.Empty<byte>();
                    if (buffer.Length > 0) {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            } finally {
                // Call base.Close() to cleanup async IO resources
                base.Dispose(disposing);
            }
        }

        // returns a bool saying whether we allocated a new array.
        private bool EnsureCapacity(int value) {
            // Check for overflow
            if (value < 0)
                throw new IOException("Stream too long");

            if (value > _capacity) {
                int newCapacity = Math.Max(value, 256);

                // We are ok with this overflowing since the next statement will deal
                // with the cases where _capacity*2 overflows.
                if (newCapacity < _capacity * 2) {
                    newCapacity = _capacity * 2;
                }

                // We want to expand the array up to Array.MaxByteArrayLength
                // And we want to give the user the value that they asked for
                if ((uint) (_capacity * 2) > MaxByteArrayLength) {
                    newCapacity = Math.Max(value, MaxByteArrayLength);
                }

                Capacity = newCapacity;
                return true;
            }

            return false;
        }

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try {
                Flush();
                return Task.CompletedTask;
            } catch (Exception ex) {
                return Task.FromException(ex);
            }
        }


        public byte[] GetBuffer() {
            return _buffer;
        }

        public bool TryGetBuffer(out ArraySegment<byte> buffer) {
            buffer = new ArraySegment<byte>(_buffer, offset: 0, count: (_length));
            return true;
        }

        // -------------- PERF: Internal functions for fast direct access of RentalMemoryStream buffer (cf. BinaryReader for usage) ---------------

        // PERF: Internal sibling of GetBuffer, always returns a buffer (cf. GetBuffer())
        internal byte[] InternalGetBuffer() {
            return _buffer;
        }

        // PERF: True cursor position, we don't need _origin for direct access
        internal int InternalGetPosition() {
            return _position;
        }

        // PERF: Expose internal buffer for BinaryReader instead of going via the regular Stream interface which requires to copy the data out
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> InternalReadSpan(int count) {
            EnsureNotClosed();

            int origPos = _position;
            int newPos = origPos + count;

            if ((uint) newPos > (uint) _length) {
                _position = _length;
                throw new EndOfStreamException();
            }

            var span = new ReadOnlySpan<byte>(_buffer, origPos, count);
            _position = newPos;
            return span;
        }

        // PERF: Get actual length of bytes available for read; do sanity checks; shift position - i.e. everything except actual copying bytes
        internal int InternalEmulateRead(int count) {
            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n < 0)
                n = 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0"); // len is less than 2^31 -1.
            _position += n;
            return n;
        }

        // Gets & sets the capacity (number of bytes allocated) for this stream.
        // The capacity cannot be set to a value less than the current length
        // of the stream.
        // 
        public int Capacity {
            get {
                EnsureNotClosed();
                return _capacity;
            }
            set {
                // Only update the capacity if the MS is expandable and the value is different than the current capacity.
                // Special behavior if the MS isn't expandable: we don't throw if value is the same as the current capacity
                if (value < Length)
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity is less than Length");;

                EnsureNotClosed();
            
                // RentalMemoryStream has this invariant: _origin > 0 => !expandable (see ctors)
                if (value > _capacity) {
                    var currentBuffer = _buffer;
                    if (value > 0) {
                        var newBuffer = ArrayPool<byte>.Shared.Rent(value);
                        if (currentBuffer.Length != 0) {
                            if (_length > 0) {
                                Buffer.BlockCopy(currentBuffer, 0, newBuffer, 0, _length);
                            }

                            ArrayPool<byte>.Shared.Return(currentBuffer);
                        }
                        _buffer = newBuffer;
                        _capacity = newBuffer.Length;
                    } else {
                        if (currentBuffer.Length != 0) {
                            ArrayPool<byte>.Shared.Return(currentBuffer);
                        }
                        _buffer = Array.Empty<byte>();
                        _capacity = 0;
                    }

                }
            }
        }

        public override long Length {
            get {
                EnsureNotClosed();
                return _length;
            }
        }

        public override long Position {
            get {
                EnsureNotClosed();
                return _position;
            }
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Position is less than 0");

                EnsureNotClosed();

                if (value > MemStreamMaxLength)
                    throw new ArgumentOutOfRangeException(nameof(value), "Position is greater than 2^31 - 1");
                _position = (int) value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer is null");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is less than 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count is less than 0");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Given buffer is too small");

            EnsureNotClosed();

            int n = _length - _position;
            if (n > count)
                n = count;
            if (n <= 0)
                return 0;

            Debug.Assert(_position + n >= 0, "_position + n >= 0"); // len is less than 2^31 -1.

            if (n <= 8) {
                int byteCount = n;
                while (--byteCount >= 0)
                    buffer[offset + byteCount] = _buffer[_position + byteCount];
            } else
                Buffer.BlockCopy(_buffer, _position, buffer, offset, n);

            _position += n;

            return n;
        }

        public override int Read(Span<byte> buffer) {
            if (GetType() != typeof(RentalMemoryStream)) {
                // RentalMemoryStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }

            EnsureNotClosed();

            int n = Math.Min(_length - _position, buffer.Length);
            if (n <= 0)
                return 0;

            // TODO https://github.com/dotnet/coreclr/issues/15076:
            // Read(byte[], int, int) has an n <= 8 optimization, presumably based
            // on benchmarking.  Determine if/where such a cut-off is here and add
            // an equivalent optimization if necessary.
            new Span<byte>(_buffer, _position, n).CopyTo(buffer);

            _position += n;
            return n;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer is null");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset is less than 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count is less than 0");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Given buffer is too small");

            // If cancellation was requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            try {
                int n = Read(buffer, offset, count);
                var t = _lastReadTask;
                Debug.Assert(t == null || t.Status == TaskStatus.RanToCompletion,
                             "Expected that a stored last task completed successfully");
                return (t != null && t.Result == n) ? t : (_lastReadTask = Task.FromResult<int>(n));
            } catch (OperationCanceledException oce) {
                return Task.FromCanceled<int>(oce.CancellationToken);
            } catch (Exception exception) {
                return Task.FromException<int>(exception);
            }
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) {
            if (cancellationToken.IsCancellationRequested) {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }

            try {
                // ReadAsync(Memory<byte>,...) needs to delegate to an existing virtual to do the work, in case an existing derived type
                // has changed or augmented the logic associated with reads.  If the Memory wraps an array, we could delegate to
                // ReadAsync(byte[], ...), but that would defeat part of the purpose, as ReadAsync(byte[], ...) often needs to allocate
                // a Task<int> for the return value, so we want to delegate to one of the synchronous methods.  We could always
                // delegate to the Read(Span<byte>) method, and that's the most efficient solution when dealing with a concrete
                // RentalMemoryStream, but if we're dealing with a type derived from RentalMemoryStream, Read(Span<byte>) will end up delegating
                // to Read(byte[], ...), which requires it to get a byte[] from ArrayPool and copy the data.  So, we special-case the
                // very common case of the Memory<byte> wrapping an array: if it does, we delegate to Read(byte[], ...) with it,
                // as that will be efficient in both cases, and we fall back to Read(Span<byte>) if the Memory<byte> wrapped something
                // else; if this is a concrete RentalMemoryStream, that'll be efficient, and only in the case where the Memory<byte> wrapped
                // something other than an array and this is a RentalMemoryStream-derived type that doesn't override Read(Span<byte>) will
                // it then fall back to doing the ArrayPool/copy behavior.
                return new ValueTask<int>(
                    MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> destinationArray) ? Read(destinationArray.Array!, destinationArray.Offset, destinationArray.Count) : Read(buffer.Span));
            } catch (OperationCanceledException oce) {
                return new ValueTask<int>(Task.FromCanceled<int>(oce.CancellationToken));
            } catch (Exception exception) {
                return new ValueTask<int>(Task.FromException<int>(exception));
            }
        }

        public override int ReadByte() {
            EnsureNotClosed();

            if (_position >= _length)
                return -1;

            return _buffer[_position++];
        }

        public override void CopyTo(Stream destination, int bufferSize) {
            // Since we did not originally override this method, validate the arguments
            // the same way Stream does for back-compat.
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be positive");
            }

            bool sourceCanRead = this.CanRead;
            if (!sourceCanRead && !this.CanWrite)
            {
                throw new ObjectDisposedException(null, "Stream is closed");
            }

            bool destinationCanWrite = destination.CanWrite;
            if (!destinationCanWrite && !destination.CanRead) {
                throw new ObjectDisposedException(nameof(destination), "Stream is closed");
            }

            if (!sourceCanRead)
            {
                throw new NotSupportedException("Stream does not support reading");
            }

            if (!destinationCanWrite)
            {
                throw new NotSupportedException("Target stream does not support writing");
            }
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Read() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Read) when we are not sure.
            if (GetType() != typeof(RentalMemoryStream)) {
                base.CopyTo(destination, bufferSize);
                return;
            }

            int originalPosition = _position;

            // Seek to the end of the RentalMemoryStream.
            int remaining = InternalEmulateRead(_length - originalPosition);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (remaining > 0) {
                // Call Write() on the other Stream, using our internal buffer and avoiding any
                // intermediary allocations.
                destination.Write(_buffer, originalPosition, remaining);
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) {
            // This implementation offers better performance compared to the base class version.

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be positive");
            }

            bool sourceCanRead = this.CanRead;
            if (!sourceCanRead && !this.CanWrite)
            {
                throw new ObjectDisposedException(null, "Stream is closed");
            }

            bool destinationCanWrite = destination.CanWrite;
            if (!destinationCanWrite && !destination.CanRead) {
                throw new ObjectDisposedException(nameof(destination), "Stream is closed");
            }

            if (!sourceCanRead)
            {
                throw new NotSupportedException("Stream does not support reading");
            }

            if (!destinationCanWrite)
            {
                throw new NotSupportedException("Target stream does not support writing");
            }
        
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to ReadAsync() which a subclass might have overridden.  
            // To be safe we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into ReadAsync) when we are not sure.
            if (GetType() != typeof(RentalMemoryStream))
                return base.CopyToAsync(destination, bufferSize, cancellationToken);

            // If cancelled - return fast:
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // Avoid copying data from this buffer into a temp buffer:
            //   (require that InternalEmulateRead does not throw,
            //    otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

            int pos = _position;
            int n = InternalEmulateRead(_length - _position);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (n == 0)
                return Task.CompletedTask;

            // If destination is not a memory stream, write there asynchronously:
            if (!(destination is RentalMemoryStream memStrDest))
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);

            try {
                // If destination is a RentalMemoryStream, CopyTo synchronously:
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;
            } catch (Exception ex) {
                return Task.FromException(ex);
            }
        }
        public void CopyTo(Stream destination) {
            // Since we did not originally override this method, validate the arguments
            // the same way Stream does for back-compat.
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            bool sourceCanRead = this.CanRead;
            if (!sourceCanRead && !this.CanWrite)
            {
                throw new ObjectDisposedException(null, "Stream is closed");
            }

            bool destinationCanWrite = destination.CanWrite;
            if (!destinationCanWrite && !destination.CanRead) {
                throw new ObjectDisposedException(nameof(destination), "Stream is closed");
            }

            if (!sourceCanRead)
            {
                throw new NotSupportedException("Stream does not support reading");
            }

            if (!destinationCanWrite)
            {
                throw new NotSupportedException("Target stream does not support writing");
            }

            int originalPosition = _position;

            // Seek to the end of the RentalMemoryStream.
            int remaining = InternalEmulateRead(_length - originalPosition);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (remaining > 0) {
                // Call Write() on the other Stream, using our internal buffer and avoiding any
                // intermediary allocations.
                destination.Write(_buffer, originalPosition, remaining);
            }
        }

        public Task CopyToAsync(Stream destination, CancellationToken cancellationToken) {
            // This implementation offers better performance compared to the base class version.

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            bool sourceCanRead = this.CanRead;
            if (!sourceCanRead && !this.CanWrite)
            {
                throw new ObjectDisposedException(null, "Stream is closed");
            }

            bool destinationCanWrite = destination.CanWrite;
            if (!destinationCanWrite && !destination.CanRead) {
                throw new ObjectDisposedException(nameof(destination), "Stream is closed");
            }

            if (!sourceCanRead)
            {
                throw new NotSupportedException("Stream does not support reading");
            }

            if (!destinationCanWrite)
            {
                throw new NotSupportedException("Target stream does not support writing");
            }
        
            // If cancelled - return fast:
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            // Avoid copying data from this buffer into a temp buffer:
            //   (require that InternalEmulateRead does not throw,
            //    otherwise it needs to be wrapped into try-catch-Task.FromException like memStrDest.Write below)

            int pos = _position;
            int n = InternalEmulateRead(_length - _position);

            // If we were already at or past the end, there's no copying to do so just quit.
            if (n == 0)
                return Task.CompletedTask;

            // If destination is not a memory stream, write there asynchronously:
            if (!(destination is RentalMemoryStream memStrDest))
                return destination.WriteAsync(_buffer, pos, n, cancellationToken);

            try {
                // If destination is a RentalMemoryStream, CopyTo synchronously:
                memStrDest.Write(_buffer, pos, n);
                return Task.CompletedTask;
            } catch (Exception ex) {
                return Task.FromException(ex);
            }
        }


        public override long Seek(long offset, SeekOrigin loc) {
            EnsureNotClosed();

            if (offset > MemStreamMaxLength)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset was out of range. Must be non-negative and less than 2^31 - origin");

            switch (loc) {
                case SeekOrigin.Begin: {
                    int tempPosition = unchecked((int) offset);
                    if (offset < 0 || tempPosition < 0)
                        throw new IOException("Seek before begin");
                    _position = tempPosition;
                    break;
                }
                case SeekOrigin.Current: {
                    int tempPosition = unchecked(_position + (int) offset);
                    if (unchecked(_position + offset) < 0 || tempPosition < 0)
                        throw new IOException("Seek before begin");
                    _position = tempPosition;
                    break;
                }
                case SeekOrigin.End: {
                    int tempPosition = unchecked(_length + (int) offset);
                    if (unchecked(_length + offset) < 0 || tempPosition < 0)
                        throw new IOException("Seek before begin");
                    _position = tempPosition;
                    break;
                }
                default:
                    throw new ArgumentException("Invalid SeekOrigin");
            }

            Debug.Assert(_position >= 0, "_position >= 0");
            return _position;
        }

        // Sets the length of the stream to a given value.  The new
        // value must be nonnegative and less than the space remaining in
        // the array, int.MaxValue - origin
        // Origin is 0 in all cases other than a RentalMemoryStream created on
        // top of an existing array and a specific starting offset was passed 
        // into the RentalMemoryStream constructor.  The upper bounds prevents any 
        // situations where a stream may be created on top of an array then 
        // the stream is made longer than the maximum possible length of the 
        // array (int.MaxValue).
        // 
        public override void SetLength(long value) {
            if (value < 0 || value > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.");

            EnsureWriteable();

            // Origin wasn't publicly exposed above.
            Debug.Assert(MemStreamMaxLength == int.MaxValue); // Check parameter validation logic in this method if this fails.
            if (value > (int.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(value), "Length was out of range. Must be non-negative and less than 2^31 - origin");

            int newLength = (int) value;
            bool allocatedNewArray = EnsureCapacity(newLength);
            if (!allocatedNewArray && newLength > _length)
                Array.Clear(_buffer, _length, newLength - _length);
            _length = newLength;
            if (_position > newLength)
                _position = newLength;
        }

        public byte[] ToArray() {
            int count = _length;
            if (count == 0)
                return Array.Empty<byte>();
            byte[] copy = new byte[count];
            Buffer.BlockCopy(_buffer, 0, copy, 0, count);
            return copy;
        }

        public void CopyTo(Span<byte> target) {
            _buffer.AsSpan(0, _position).CopyTo(target);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

            EnsureNotClosed();
            EnsureWriteable();

            int i = _position + count;
            // Check for overflow
            if (i < 0)
                throw new IOException("Stream too long");

            if (i > _length) {
                bool mustZero = _position > _length;
                if (i > _capacity) {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray) {
                        mustZero = false;
                    }
                }

                if (mustZero) {
                    Array.Clear(_buffer, _length, i - _length);
                }

                _length = i;
            }

            if ((count <= 8) && (buffer != _buffer)) {
                int byteCount = count;
                while (--byteCount >= 0) {
                    _buffer[_position + byteCount] = buffer[offset + byteCount];
                }
            } else {
                Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            }

            _position = i;
        }

        public override void Write(ReadOnlySpan<byte> buffer) {
            if (GetType() != typeof(RentalMemoryStream)) {
                // RentalMemoryStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(Span<byte>) overload being introduced.  In that case, this Write(Span<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
                return;
            }

            EnsureNotClosed();
            EnsureWriteable();

            // Check for overflow
            int i = _position + buffer.Length;
            if (i < 0)
                throw new IOException("Stream too long");

            if (i > _length) {
                bool mustZero = _position > _length;
                if (i > _capacity) {
                    bool allocatedNewArray = EnsureCapacity(i);
                    if (allocatedNewArray) {
                        mustZero = false;
                    }
                }

                if (mustZero) {
                    Array.Clear(_buffer, _length, i - _length);
                }

                _length = i;
            }

            buffer.CopyTo(new Span<byte>(_buffer, _position, buffer.Length));
            _position = i;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Buffer cannot be null");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (buffer.Length - offset < count)
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");

            // If cancellation is already requested, bail early
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            try {
                Write(buffer, offset, count);
                return Task.CompletedTask;
            } catch (OperationCanceledException oce) {
                return Task.FromCanceled(oce.CancellationToken);
            } catch (Exception exception) {
                return Task.FromException(exception);
            }
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
            if (cancellationToken.IsCancellationRequested) {
                return new ValueTask(Task.FromCanceled(cancellationToken));
            }

            try {
                // See corresponding comment in ReadAsync for why we don't just always use Write(ReadOnlySpan<byte>).
                // Unlike ReadAsync, we could delegate to WriteAsync(byte[], ...) here, but we don't for consistency.
                if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> sourceArray)) {
                    Write(sourceArray.Array!, sourceArray.Offset, sourceArray.Count);
                } else {
                    Write(buffer.Span);
                }

                return default;
            } catch (OperationCanceledException oce) {
                return new ValueTask(Task.FromCanceled(oce.CancellationToken));
            } catch (Exception exception) {
                return new ValueTask(Task.FromException(exception));
            }
        }

        public override void WriteByte(byte value) {
            EnsureNotClosed();
            EnsureWriteable();

            if (_position >= _length) {
                int newLength = _position + 1;
                bool mustZero = _position > _length;
                if (newLength >= _capacity) {
                    bool allocatedNewArray = EnsureCapacity(newLength);
                    if (allocatedNewArray) {
                        mustZero = false;
                    }
                }

                if (mustZero) {
                    Array.Clear(_buffer, _length, _position - _length);
                }

                _length = newLength;
            }

            _buffer[_position++] = value;
        }

        // Writes this RentalMemoryStream to another stream.
        public void WriteTo(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null");

            EnsureNotClosed();

            stream.Write(_buffer, 0, _length);
        }
    }
}