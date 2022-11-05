using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Nucs.Collections;

public sealed class ByteBufferView {
    public readonly byte[] _array;

    public ByteBufferView(ByteBuffer span) {
        _array = span.ToArray();
    }

    public ByteBufferView(ReadOnlySpan<byte> span) {
        _array = span.ToArray();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public byte[] Items => _array;
}

/// <summary>
/// Span represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
/// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
/// </summary>
[DebuggerTypeProxy(typeof(ByteBufferView))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly unsafe ref struct ByteBuffer {
    /// <summary>A byref or a native ptr.</summary>
    public readonly byte* Pointer;

    /// <summary>The number of elements this Span contains.</summary>
    public readonly long Length;

    /// <summary>
    /// Creates a new span over the entirety of the target array.
    /// </summary>
    /// <param name="array">The target array.</param>
    /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
    /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBuffer(byte* buffer, long bufferSize) : this(buffer, bufferSize, 0, bufferSize) { }

    /// <summary>
    /// Creates a new span over the portion of the target array beginning
    /// at 'start' index and ending at 'end' index (exclusive).
    /// </summary>
    /// <param name="buffer">The target array.</param>
    /// <param name="bufferSize">The size of the buffer</param>
    /// <param name="start">The index at which to begin the span.</param>
    /// <param name="length">The number of items in the span.</param>
    /// <remarks>Returns default when <paramref name="buffer"/> is null.</remarks>
    /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="buffer"/> is covariant and array's type is not exactly T[].</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ByteBuffer(byte* buffer, long bufferSize, long start, long length) {
        if (start > bufferSize || length > bufferSize - start)
            throw new ArgumentOutOfRangeException();

        Pointer = buffer + start;
        Length = length;
    }

    /// <summary>
    /// Returns a reference to specified element of the Span.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="System.IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to Length
    /// </exception>
    public unsafe ref byte this[long index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (index >= Length)
                throw new ArgumentOutOfRangeException();
            return ref Pointer[index];
        }
    }

    /// <summary>
    /// Returns true if Length is 0.
    /// </summary>
    public bool IsEmpty {
        get => 0 >= Length; // Workaround for https://github.com/dotnet/runtime/issues/10950
    }

    /// <summary>
    /// Returns false if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator !=(ByteBuffer left, ByteBuffer right) =>
        !(left == right);

    /// <summary>
    /// This method is not supported as spans cannot be boxed. To compare two spans, use operator==.
    /// <exception cref="System.NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    /// </summary>
    [Obsolete("Equals() on Span will always throw an exception. Use == instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) =>
        throw new NotSupportedException();

    /// <summary>
    /// This method is not supported as spans cannot be boxed.
    /// <exception cref="System.NotSupportedException">
    /// Always thrown by this method.
    /// </exception>
    /// </summary>
    [Obsolete("GetHashCode() on Span will always throw an exception.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() =>
        throw new NotSupportedException();

    /// <summary>
    /// Returns an empty <see cref="Span{T}"/>
    /// </summary>
    public static ByteBuffer Empty => default;

    /// <summary>Gets an enumerator for this span.</summary>
    public Enumerator GetEnumerator() =>
        new Enumerator(this);

    /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
    public ref struct Enumerator {
        /// <summary>The span being enumerated.</summary>
        public readonly ByteBuffer _span;

        /// <summary>The next index to yield.</summary>
        public long _index;

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(ByteBuffer span) {
            _span = span;
            _index = -1;
        }

        /// <summary>Advances the enumerator to the next element of the span.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() {
            long index = _index + 1;
            if (index < _span.Length) {
                _index = index;
                return true;
            }

            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        public ref byte Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    /// <summary>
    /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
    /// It can be used for pinning and is required to support the use of span within a fixed statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref byte GetPinnableReference() {
        // Ensure that the native code has just one forward branch that is predicted-not-taken.
        ref byte ret = ref Unsafe.NullRef<byte>();
        if (Length != 0) ret = ref Pointer[0];
        return ref ret;
    }

    //TODO:
    /*/// <summary>
    /// Clears the contents of this span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Clear() {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) {
            SpanHelpers.ClearWithReferences(ref Unsafe.As<T, IntPtr>(ref _pointer.Value), _length * (Unsafe.SizeOf<T>() / sizeof(nuint)));
        } else {
            SpanHelpers.ClearWithoutReferences(ref Unsafe.As<T, byte>(ref _pointer.Value), _length * Unsafe.SizeOf<T>());
        }
    }*/


    /// <summary>
    /// Copies the contents of this span into destination span. If the source
    /// and destinations overlap, this method behaves as if the original values in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The span to copy items into.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when the destination Span is shorter than the source Span.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void CopyTo(ByteBuffer destination) {
        // Using "if (!TryCopyTo(...))" results in two branches: one for the length
        // check, and one for the result of TryCopyTo. Since these checks are equivalent,
        // we can optimize by performing the check once ourselves then calling Memmove directly.

        if (Length <= destination.Length) {
            Buffer.MemoryCopy(Pointer, destination.Pointer, Length, Length);
        } else {
            throw new ArgumentException("Dest too small");
        }
    }

    /// <summary>
    /// Copies the contents of this span into destination span. If the source
    /// and destinations overlap, this method behaves as if the original values in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The span to copy items into.</param>
    /// <returns>If the destination span is shorter than the source span, this method
    /// return false and no data is written to the destination.</returns>
    public unsafe bool TryCopyTo(ByteBuffer destination) {
        bool retVal = false;
        if (Length <= destination.Length) {
            Buffer.MemoryCopy(Pointer, destination.Pointer, destination.Length, Length);
            retVal = true;
        }

        return retVal;
    }

    /// <summary>
    /// Returns true if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator ==(ByteBuffer left, ByteBuffer right) =>
        left.Length == right.Length &&
        Unsafe.AreSame<byte>(ref Unsafe.AsRef<byte>(left.Pointer), ref Unsafe.AsRef<byte>(right.Pointer));

    /// <summary>
    /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
    /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
    /// </summary>
    public override string ToString() {
        return string.Format("LongSpan<{0}>[{1}]", typeof(byte).Name, Length);
    }

    /// <summary>
    /// Forms a slice out of the given span, beginning at 'start'.
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ByteBuffer Slice(long start) {
        if (start > Length)
            throw new ArgumentOutOfRangeException();

        return new ByteBuffer(Pointer + start, Length - start);
    }

    /// <summary>
    /// Forms a slice out of the given span, beginning at 'start'.
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ByteBuffer Slice(int start) {
        if (start > Length)
            throw new ArgumentOutOfRangeException();

        return new ByteBuffer(Pointer + start, Length - start);
    }

    /// <summary>
    /// Forms a slice out of the given span, beginning at 'start', of given length
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <param name="length">The desired length for the slice (exclusive).</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ByteBuffer Slice(long start, long length) {
        if (start > Length || length > Length - start)
            throw new ArgumentOutOfRangeException();

        return new ByteBuffer(Pointer + start, length);
    }

    /// <summary>
    /// Copies the contents of this span into a new array.  This heap
    /// allocates, so should generally be avoided, however it is sometimes
    /// necessary to bridge the gap with APIs written in terms of arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe byte[] ToArray() {
        if (Length == 0)
            return Array.Empty<byte>();

        var destination = new byte[Length];
        Buffer.MemoryCopy(Pointer, Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(destination)), Length, Length);
        return destination;
    }

    /// <summary>
    ///     Will try to fit current ByteBuffer into a span supporting up to int.MaxValue bytes.
    /// </summary>
    /// <returns></returns>
    public Span<byte> TryToSpan() {
        return new Span<byte>(Pointer, checked((int) Length));
    }
}