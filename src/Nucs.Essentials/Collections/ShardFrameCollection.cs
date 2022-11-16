using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Nucs.Exceptions;

namespace Nucs.Collections.Structs;

public delegate void FrameVisitor(long frameIndex, ByteBuffer frame);
public delegate void FrameVisitor<in TState>(long frameIndex, TState state, ByteBuffer frame);

internal sealed class ShardFrameDebugViewer {
    private class DisplayHandle : SafeBuffer {
        public DisplayHandle(IntPtr handle, uint len) : base(false) {
            SetHandle(handle);
            Initialize(len);
        }

        protected override bool ReleaseHandle() {
            return false;
        }
    }

    public ShardFrameDebugViewer(ShardFrameCollection shard) {
        unsafe {
            Buckets = shard.Buckets;
            Buffer = new UnmanagedMemoryAccessor(new DisplayHandle((IntPtr) shard.Buffer, (uint) unchecked(shard.BytesLength % uint.MaxValue)), 0, shard.BytesLength);
            Array = new Memory<byte>(new Span<byte>(shard.Buffer, (int) unchecked(shard.BytesLength % uint.MaxValue)).ToArray());
        }
    }

    public StructList<ShardFrameCollection.BucketIndex> Buckets { get; set; }
    public UnmanagedMemoryAccessor Buffer { get; set; }
    public Memory<byte> Array { get; set; }
}

/// <summary>
///     Stores a collection of frames atwhich each frame can be sized differently. The distance is delimited by a bucket object keeping track of the offset every bucketSize parameter.
///     A frame can be int.MaxValue long and a frame collection buffer can be long.MaxValue long.
/// </summary>
[DebuggerTypeProxy(typeof(ShardFrameDebugViewer))]
[DebuggerDisplay("{ToString(),raw}")]
public class ShardFrameCollection : IDisposable {
    protected long _length;
    public unsafe byte* Buffer;
    protected readonly int _bucketSize;
    protected readonly bool _supportBufferExpansion;
    protected readonly float _bufferExpansionFactor;
    public StructList<BucketIndex> Buckets;
    public readonly Encoding Encoding;
    protected long _bufferSize;
    protected long _bytesLength;

    /// <summary>
    ///     The number of frames this shard holds in total.
    /// </summary>
    public long Count => _length;

    /// <summary>
    ///     The total buffer size in bytes.
    /// </summary>
    public long BufferSize => _bufferSize;

    /// <summary>
    ///     The length of bytes written to this shard collection so far.
    /// </summary>
    public long BytesLength => _bytesLength;

    [StructLayout(LayoutKind.Explicit)]
    public struct BucketIndex {
        [FieldOffset(0)]
        public readonly long Offset;

        [FieldOffset(sizeof(long))]
        public int TotalLength;

        public readonly long EndOffset => Offset + TotalLength;

        public BucketIndex(long offset, int totalLength) {
            Offset = offset;
            TotalLength = totalLength;
        }
    }

    public unsafe ShardFrameCollection(long bufferSize, int bucketSize, bool supportBufferExpansion = false, float bufferExpansionFactor = 1.5f, Encoding? encoding = null) {
        if (bufferSize < _bytesLength)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bufferSize));

        Buffer = (byte*) Marshal.AllocHGlobal(new IntPtr(bufferSize));
        _bufferSize = bufferSize;
        _bucketSize = bucketSize;
        _supportBufferExpansion = supportBufferExpansion;
        _bufferExpansionFactor = bufferExpansionFactor;
        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };

        if (encoding != null)
            Encoding = encoding;
        else
            Encoding = Encoding.UTF8;
    }

    public unsafe void Add(ReadOnlySpan<byte> data) {
        int subBucketIndex = checked((int) (_length / _bucketSize));
        (BucketIndex[] buckets, int bucketsCount) = Buckets;
        if (bucketsCount == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(buckets[subBucketIndex - 1].EndOffset, 0));
            buckets = Buckets.InternalArray;
        }

        ref BucketIndex bucket = ref buckets[subBucketIndex];
        int frameSize = sizeof(int) + data.Length;
        var endOffset = bucket.EndOffset;
        EnsureCapacity(endOffset + frameSize);
        fixed (byte* source = data) {
            int* dest = (int*) (Buffer + endOffset);
            *dest = data.Length;

            System.Buffer.MemoryCopy(source: source, destination: &dest[1],
                                     destinationSizeInBytes: _bufferSize - endOffset,
                                     sourceBytesToCopy: data.Length);
        }

        _length++;
        bucket.TotalLength += frameSize;
        _bytesLength += frameSize;
    }

    public unsafe void Add(ReadOnlySpan<char> str) {
        int subBucketIndex = checked((int) (_length / _bucketSize));
        (BucketIndex[] buckets, int bucketsCount) = Buckets;
        if (bucketsCount == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(buckets[subBucketIndex - 1].EndOffset, 0));
            buckets = Buckets.InternalArray;
        }

        ref BucketIndex bucket = ref buckets[subBucketIndex];
        var endOffset = bucket.EndOffset;
        if (endOffset + str.Length * 4 /*peak length any encoding*/ + sizeof(int) > _bufferSize)
            EnsureCapacity(endOffset + Encoding.GetByteCount(str) + sizeof(int));

        int* dest = (int*) (Buffer + endOffset);
        var read = Encoding.GetBytes(chars: str, bytes: new Span<byte>(&dest[1], unchecked((int) Math.Min(int.MaxValue, (_bufferSize - endOffset - sizeof(int))))));
        *dest = read;
        var frameSize = sizeof(int) + read;

        _length++;
        bucket.TotalLength += frameSize;
        _bytesLength += frameSize;
    }

    public unsafe void Add(byte* source, int length) {
        int subBucketIndex = checked((int) (_length / _bucketSize));
        (BucketIndex[] buckets, int bucketsCount) = Buckets;
        if (bucketsCount == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(buckets[subBucketIndex - 1].EndOffset, 0));
            buckets = Buckets.InternalArray;
        }

        ref BucketIndex bucket = ref buckets[subBucketIndex];
        var endOffset = bucket.EndOffset;
        int frameSize = sizeof(int) + length;
        EnsureCapacity(endOffset + frameSize);

        int* dest = (int*) (Buffer + endOffset);
        *dest = length;

        System.Buffer.MemoryCopy(source: source, destination: &dest[1],
                                 destinationSizeInBytes: _bufferSize - endOffset,
                                 sourceBytesToCopy: length);

        _length++;
        bucket.TotalLength += frameSize;
        _bytesLength += frameSize;
    }

    protected unsafe void EnsureCapacity(long expectedSize) {
        if (expectedSize <= _bufferSize) return;
        if (!_supportBufferExpansion)
            throw new OutOfMemoryException("Buffer is full");

        long newLen = _bufferSize;
        do {
            var len = (long) Math.Ceiling(newLen * _bufferExpansionFactor);
            if (len == newLen) //failsafe
                len++;
            newLen = len;
        } while (expectedSize > newLen);

        Nucs.Extensions.Collections.ResizeMarshalled(ref Buffer, ref _bufferSize, newLen, true);
    }

    /// <summary>
    ///     Returns all frames with length at the beginning of each frame (lenght included in each frame).
    /// </summary>
    /// <param name="startIndex">The frame index, 0 based.</param>
    /// <param name="count">Count, 1 based</param>
    public unsafe ByteBuffer GetRange(long startIndex, long count) {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        var left = this[startIndex, includeLength: true];
        var right = this[startIndex + count - 1, includeLength: true];

        //target is frame at offset
        return new ByteBuffer(left.Pointer, right.Pointer + right.Length - left.Pointer);
    }

    /// <summary>
    ///     Returns the frame at the specified index.
    /// </summary>
    /// <param name="frameIndex">The frame index, 0 based.</param>
    /// <param name="includeLength">Should the returned <see cref="ByteBuffer"/> include 4 bytes at the beggining with the length of the frame (for transmittion purposes)</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public unsafe ByteBuffer this[long frameIndex, bool includeLength = false] {
        get {
            if (frameIndex < 0 || frameIndex >= _length)
                throw new IndexOutOfRangeException();

            byte* buffer = Buffer;
            long offset = Buckets[frameIndex / _bucketSize].Offset;
            long jumps = frameIndex % _bucketSize;
            for (long i = 0; i < jumps; i++) {
                offset += sizeof(int) + *((int*) (buffer + offset));
            }

            //target is frame at offset
            var frameLength = *((int*) (buffer + offset));
            if (includeLength)
                return new ByteBuffer(Buffer, _bufferSize, offset, frameLength + sizeof(int));
            else
                return new ByteBuffer(Buffer, _bufferSize, offset + sizeof(int), frameLength);
        }
    }

    public unsafe ByteBuffer this[int frameIndex, bool includeLength = false] {
        get {
            if (frameIndex < 0 || frameIndex >= _length)
                throw new IndexOutOfRangeException();

            byte* buffer = Buffer;
            long offset = Buckets[frameIndex / _bucketSize].Offset;
            long jumps = frameIndex % _bucketSize;
            for (long i = 0; i < jumps; i++) {
                offset += sizeof(int) + *((int*) (buffer + offset));
            }

            //target is frame at offset
            var frameLength = *((int*) (buffer + offset));
            if (includeLength)
                return new ByteBuffer(Buffer, _bufferSize, offset, frameLength + sizeof(int));
            else
                return new ByteBuffer(Buffer, _bufferSize, offset + sizeof(int), frameLength);
        }
    }


    public unsafe void Iterate(long startIndex, long endIndex, FrameVisitor visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        byte* buffer = Buffer;
        //skip to needed offset
        long offset = Buckets[startIndex / _bucketSize].Offset;
        long jumps = startIndex % _bucketSize;
        for (long i = 0; i < jumps; i++) {
            offset += sizeof(int) + *((int*) (buffer + offset));
        }

        //iterate
        long step;
        long steps = endIndex - startIndex;
        for (step = 0; step < steps; step++) {
            //target is frame at offset
            visitor(step, new ByteBuffer(buffer, _bufferSize, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
            offset += sizeof(int) + *((int*) (buffer + offset));
        }
    }

    public unsafe void Iterate<TState>(long startIndex, long endIndex, TState state, FrameVisitor<TState> visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        byte* buffer = Buffer;
        //skip to needed offset
        long offset = Buckets[startIndex / _bucketSize].Offset;
        long jumps = startIndex % _bucketSize;
        for (long i = 0; i < jumps; i++) {
            offset += sizeof(int) + *((int*) (buffer + offset));
        }

        //iterate
        long step;
        long steps = endIndex - startIndex;
        for (step = 0; step < steps; step++) {
            //target is frame at offset
            visitor(step, state, new ByteBuffer(Buffer, _bufferSize, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
            offset += sizeof(int) + *((int*) (buffer + offset));
        }
    }

    /// <summary>
    ///     Attempts to return a span from internal memory if bytes length is less than <see cref="int.MaxValue"/>
    /// </summary>
    public unsafe Span<byte> TryAsSpan() {
        if (_length > int.MaxValue)
            throw new ArgumentOutOfRangeException($"Unable to cast to span, length is {_length} which is larger than storable int.MaxValue");

        return new Span<byte>(Buffer, (int) _length);
    }

    /// <summary>
    ///     Attempts to return a span from internal memory if bytes length is less than <see cref="int.MaxValue"/>
    /// </summary>
    public unsafe Span<byte> TryAsSpan(long startIndex, int length) {
        if (_length > int.MaxValue)
            throw new ArgumentOutOfRangeException($"Unable to cast to span, length is {_length} which is larger than storable int.MaxValue");

        return new Span<byte>(Buffer + startIndex, Math.Min((int) _length, length));
    }

    public void Clear(long? newBufferSize = null) {
        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };

        if (newBufferSize.HasValue) {
            unsafe {
                Extensions.Collections.ResizeMarshalled(ref Buffer, ref _bufferSize, newBufferSize.Value, true);
            }
        }
    }

    private unsafe void ReleaseUnmanagedResources() {
        Marshal.FreeHGlobal((IntPtr) Buffer);
    }

    public void Dispose() {
        Buckets.Dispose();
        ReleaseUnmanagedResources();

        GC.SuppressFinalize(this);
    }

    ~ShardFrameCollection() {
        ReleaseUnmanagedResources();
    }
}