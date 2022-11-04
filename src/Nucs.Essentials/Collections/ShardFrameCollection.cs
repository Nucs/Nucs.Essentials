using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Exceptions;

namespace Nucs.Collections.Structs;

public delegate void FrameVisitor(int frameIndex, ByteBuffer frame);
public delegate void FrameVisitor<in TState>(int frameIndex, TState state, ByteBuffer frame);

/// <summary>
///     Stores a collection of frames atwhich each frame can be sized differently. The distance is delimited by a bucket object keeping track of the offset every bucketSize parameter.
///     A frame can be int.MaxValue long and a frame collection buffer can be long.MaxValue long.
/// </summary>
public class ShardFrameCollection : IDisposable {
    protected int _length;
    public int Count => _length;
    public unsafe byte* Buffer;
    public readonly long BufferSize;
    protected readonly int _bucketSize;
    protected readonly bool _supportBufferExpansion;
    protected readonly float _bufferExpansionFactor;
    public long DataEndOffset;
    public StructList<BucketIndex> Buckets;

    public unsafe ShardFrameCollection(long bufferSize, int bucketSize, bool supportBufferExpansion = false, float bufferExpansionFactor = 1.5f) {
        if (bufferSize < DataEndOffset)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(bufferSize));

        Buffer = (byte*) Marshal.AllocHGlobal(new IntPtr(bufferSize));
        BufferSize = bufferSize;
        _bucketSize = bucketSize;
        _supportBufferExpansion = supportBufferExpansion;
        _bufferExpansionFactor = bufferExpansionFactor;
        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };
    }

    public unsafe void Add(ReadOnlySpan<byte> data) {
        int subBucketIndex = _length / _bucketSize;
        if (Buckets.Length == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(Buckets[subBucketIndex - 1].EndOffset, 0));
        }

        ref BucketIndex bucket = ref Buckets[subBucketIndex];
        int frameSize = sizeof(int) + data.Length;
        EnsureCapacity(bucket.EndOffset + frameSize);

        fixed (byte* source = data) {
            int* dest = (int*) (Buffer + bucket.EndOffset);
            *dest = data.Length;

            System.Buffer.MemoryCopy(source: source, destination: &dest[1],
                                     destinationSizeInBytes: BufferSize - bucket.EndOffset,
                                     sourceBytesToCopy: data.Length);
        }

        _length++;
        bucket.TotalLength += frameSize;
        DataEndOffset += frameSize;
    }

    public unsafe void Add(byte* source, int length) {
        int subBucketIndex = _length / _bucketSize;
        if (Buckets.Length == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(Buckets[subBucketIndex - 1].EndOffset, 0));
        }

        ref BucketIndex bucket = ref Buckets[subBucketIndex];
        int frameSize = sizeof(int) + length;
        EnsureCapacity(bucket.EndOffset + frameSize);

        int* dest = (int*) (Buffer + bucket.EndOffset);
        *dest = length;

        System.Buffer.MemoryCopy(source: source, destination: &dest[1],
                                 destinationSizeInBytes: BufferSize - bucket.EndOffset,
                                 sourceBytesToCopy: length);

        _length++;
        bucket.TotalLength += frameSize;
        DataEndOffset += frameSize;
    }

    protected unsafe void EnsureCapacity(long expectedSize) {
        if (expectedSize <= BufferSize) return;
        if (!_supportBufferExpansion)
            throw new OutOfMemoryException("Buffer is full");

        long newLen = BufferSize;
        do {
            var len = (long) Math.Ceiling(newLen * _bufferExpansionFactor);
            if (len == newLen) //failsafe
                len++;
            newLen = len;
        } while (expectedSize > newLen);

        Nucs.Extensions.Collections.ResizeMarshalled(ref Buffer, BufferSize, newLen, true);
    }

    public unsafe ByteBuffer this[long frameIndex] {
        get {
            if (frameIndex < 0 || frameIndex >= _length)
                throw new IndexOutOfRangeException();

            byte* buffer = Buffer;
            long offset = Buckets[frameIndex / _bucketSize].Offset;
            var jumps = frameIndex % _bucketSize;
            for (int i = 0; i < jumps; i++) {
                offset += sizeof(int) + *((int*) (buffer + offset));
            }

            //target is frame at offset
            var frameLength = *((int*) (buffer + offset));
            return new ByteBuffer(Buffer, BufferSize, offset + sizeof(int), frameLength);
        }
    }

    public unsafe void Iterate(long startIndex, long endIndex, FrameVisitor visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        var buffer = Buffer;
        //skip to needed offset
        long offset = Buckets[startIndex / _bucketSize].Offset;
        var jumps = startIndex % _bucketSize;
        for (long i = 0; i < jumps; i++) {
            offset += sizeof(int) + *((int*) (buffer + offset));
        }

        //iterate
        int step;
        var steps = endIndex - startIndex;
        for (step = 0; step < steps; step++) {
            //target is frame at offset
            visitor(step, new ByteBuffer(buffer, BufferSize, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
            offset += sizeof(int) + *((int*) (buffer + offset));
        }
    }

    public unsafe void Iterate<TState>(int startIndex, int endIndex, TState state, FrameVisitor<TState> visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        byte* buffer = Buffer;
        //skip to needed offset
        long offset = Buckets[startIndex / _bucketSize].Offset;
        var jumps = startIndex % _bucketSize;
        for (int i = 0; i < jumps; i++) {
            offset += sizeof(int) + *((int*) (buffer + offset));
        }

        //iterate
        int step;
        var steps = endIndex - startIndex;
        for (step = 0; step < steps; step++) {
            //target is frame at offset
            visitor(step, state, new ByteBuffer(Buffer, BufferSize, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
            offset += sizeof(int) + *((int*) (buffer + offset));
        }
    }

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

    public void Reset() {
        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };
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