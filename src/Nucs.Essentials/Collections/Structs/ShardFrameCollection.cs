using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nucs.Exceptions;

namespace Nucs.Collections.Structs;

public delegate void FrameVisitor(int frameIndex, LongSpan<byte> frame);
public delegate void FrameVisitor<in TState>(int frameIndex, TState state, LongSpan<byte> frame);

/// <summary>
///     Stores a collection of frames atwhich each frame can be sized differently. The distance is delimited by a bucket object keeping track of the offset every bucketSize parameter.
///     A frame can be int.MaxValue long and a frame collection buffer can be long.MaxValue long.
/// </summary>
public class ShardFrameCollection : IDisposable {
    protected int _length;
    public int Count => _length;
    public byte[] Buffer;
    protected readonly int _bucketSize;
    protected readonly bool _supportBufferExpansion;
    protected readonly float _bufferExpansionFactor;
    public long DataEndOffset;
    public StructList<BucketIndex> Buckets;

    public ShardFrameCollection(byte[] buffer, int bucketSize, bool supportBufferExpansion = false, float bufferExpansionFactor = 1.5f) {
        if (buffer.Length < DataEndOffset)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(buffer));

        Buffer = buffer;
        _bucketSize = bucketSize;
        _supportBufferExpansion = supportBufferExpansion;
        _bufferExpansionFactor = bufferExpansionFactor;
        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };
    }

    public unsafe BucketIndex Add(ReadOnlySpan<byte> data) {
        int subBucketIndex = _length / _bucketSize;
        if (Buckets.Length == subBucketIndex) {
            //we need to produce a new bucket
            Buckets.Add(new BucketIndex(Buckets[subBucketIndex - 1].EndOffset, 0));
        }

        ref BucketIndex bucket = ref Buckets[subBucketIndex];
        int frameSize = sizeof(int) + data.Length;
        EnsureCapacity(bucket.EndOffset + frameSize);

        fixed (byte* destBuffer = Buffer)
        fixed (byte* source = data) {
            int* dest = (int*) (destBuffer + bucket.EndOffset);
            *dest = data.Length;

            System.Buffer.MemoryCopy(source: source, destination: &dest[1],
                                     destinationSizeInBytes: Buffer.LongLength - bucket.EndOffset,
                                     sourceBytesToCopy: data.Length);
        }

        _length++;
        bucket.TotalLength += frameSize;
        DataEndOffset += frameSize;
        return bucket;
    }

    protected void EnsureCapacity(long expectedSize) {
        if (expectedSize <= Buffer.LongLength) return;
        if (!_supportBufferExpansion)
            throw new OutOfMemoryException("Buffer is full");

        long newLen = Buffer.LongLength;
        do {
            var len = (long) Math.Ceiling(newLen * _bufferExpansionFactor);
            if (len == newLen) //failsafe
                len++;
            newLen = len;
        } while (expectedSize > newLen);

        Nucs.Extensions.Collections.Resize(ref Buffer, newLen);
    }

    public unsafe LongSpan<byte> this[int frameIndex] {
        get {
            if (frameIndex < 0 || frameIndex >= _length)
                throw new IndexOutOfRangeException();

            fixed (byte* buffer = Buffer) {
                long offset = Buckets[frameIndex / _bucketSize].Offset;
                var jumps = frameIndex % _bucketSize;
                for (int i = 0; i < jumps; i++) {
                    offset += sizeof(int) + *((int*) (buffer + offset));
                }

                //target is frame at offset
                var frameLength = *((int*) (buffer + offset));
                return new LongSpan<byte>(Buffer, offset + sizeof(int), frameLength);
            }
        }
    }

    public unsafe void Iterate(int startIndex, int endIndex, FrameVisitor visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        fixed (byte* buffer = Buffer) {
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
                visitor(step, new LongSpan<byte>(Buffer, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
                offset += sizeof(int) + *((int*) (buffer + offset));
            }
        }
    }

    public unsafe void Iterate<TState>(int startIndex, int endIndex, TState state, FrameVisitor<TState> visitor) {
        if (startIndex < 0 || startIndex + endIndex > _length)
            throw new IndexOutOfRangeException();

        fixed (byte* buffer = Buffer) {
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
                visitor(step, state, new LongSpan<byte>(Buffer, offset + sizeof(int), /*frameSize: */ *((int*) (buffer + offset))));
                offset += sizeof(int) + *((int*) (buffer + offset));
            }
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

    public void Reset(bool clearBuffer = false) {
        if (clearBuffer)
            Array.Fill(Buffer, default);

        Buckets = new StructList<BucketIndex>(4) {
            new BucketIndex(0L, 0)
        };
    }

    public void Dispose() {
        Buckets.Dispose();
        Buffer = null;
    }
}