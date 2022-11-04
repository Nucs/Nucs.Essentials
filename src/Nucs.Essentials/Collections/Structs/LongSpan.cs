using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nucs.Collections.Structs;

public sealed class SpanDebugView<T> {
    public readonly T[] _array;

    public SpanDebugView(LongSpan<T> span) {
        _array = span.ToArray();
    }

    public SpanDebugView(ReadOnlySpan<T> span) {
        _array = span.ToArray();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => _array;
}

// ByReference<T> is meant to be used to represent "ref T" fields. It is working
// around lack of first class support for byref fields in C# and IL. The JIT and
// type loader has special handling for it that turns it into a thin wrapper around ref T.
public readonly ref struct ByReference<T> {
    #pragma warning disable CA1823, 169 // public field '{blah}' is never used
    public readonly IntPtr _value;
    #pragma warning restore CA1823, 169

    public unsafe ByReference(ref T value) {
        // Implemented as a JIT intrinsic - This default implementation is for
        // completeness and to provide a concrete error if called via reflection
        // or if intrinsic is missed.
        _value = new IntPtr(Unsafe.AsPointer(ref value));
    }

    public unsafe ref T Value {
        // Implemented as a JIT intrinsic - This default implementation is for
        // completeness and to provide a concrete error if called via reflection
        // or if the intrinsic is missed.

        get => ref Unsafe.AsRef<T>(_value.ToPointer());
    }
}

/// <summary>
/// Span represents a contiguous region of arbitrary memory. Unlike arrays, it can point to either managed
/// or native memory, or to memory allocated on the stack. It is type- and memory-safe.
/// </summary>
[DebuggerTypeProxy(typeof(SpanDebugView<>))]
[DebuggerDisplay("{ToString(),raw}")]
public readonly ref struct LongSpan<T> {
    /// <summary>A byref or a native ptr.</summary>
    public readonly ByReference<T> _pointer;

    /// <summary>The number of elements this Span contains.</summary>
    public readonly long _length;

    /// <summary>
    /// Creates a new span over the entirety of the target array.
    /// </summary>
    /// <param name="array">The target array.</param>
    /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
    /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LongSpan(T[]? array) {
        if (array == null) {
            this = default;
            return; // returns default
        }

        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            throw new ArrayTypeMismatchException();

        _pointer = new ByReference<T>(ref MemoryMarshal.GetArrayDataReference(array));
        _length = array.Length;
    }

    /// <summary>
    /// Creates a new span over the portion of the target array beginning
    /// at 'start' index and ending at 'end' index (exclusive).
    /// </summary>
    /// <param name="array">The target array.</param>
    /// <param name="start">The index at which to begin the span.</param>
    /// <param name="length">The number of items in the span.</param>
    /// <remarks>Returns default when <paramref name="array"/> is null.</remarks>
    /// <exception cref="System.ArrayTypeMismatchException">Thrown when <paramref name="array"/> is covariant and array's type is not exactly T[].</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in the range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LongSpan(T[]? array, long start, long length) {
        if (array == null) {
            if (start != 0 || length != 0)
                throw new ArgumentOutOfRangeException();
            this = default;
            return; // returns default
        }

        if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            throw new ArrayTypeMismatchException();

        if (start > array.Length || length > (array.Length - start))
            throw new ArgumentOutOfRangeException();

        _pointer = new ByReference<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), new IntPtr(start)));
        _length = length;
    }

    /// <summary>
    /// Creates a new span over the target unmanaged buffer.  Clearly this
    /// is quite dangerous, because we are creating arbitrarily typed T's
    /// out of a void*-typed block of memory.  And the length is not checked.
    /// But if this creation is correct, then all subsequent uses are correct.
    /// </summary>
    /// <param name="pointer">An unmanaged pointer to memory.</param>
    /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <typeparamref name="T"/> is reference type or contains pointers and hence cannot be stored in unmanaged memory.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="length"/> is negative.
    /// </exception>
    [CLSCompliant(false)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe LongSpan(void* pointer, long length) {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            throw new ArrayTypeMismatchException("ThrowInvalidTypeWithPointersNotSupported");
        if (length < 0)
            throw new ArgumentOutOfRangeException();

        _pointer = new ByReference<T>(ref Unsafe.As<byte, T>(ref *(byte*) pointer));
        _length = length;
    }

    // Constructor for public use only.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LongSpan(ref T ptr, long length) {
        Debug.Assert(length >= 0);

        _pointer = new ByReference<T>(ref ptr);
        _length = length;
    }
    
    /// <summary>
    /// Returns a reference to specified element of the Span.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="System.IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to Length
    /// </exception>
    public unsafe ref T this[long index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (index >= _length)
                throw new ArgumentOutOfRangeException();
            return ref Unsafe.Add<T>(ref _pointer.Value, new IntPtr(index));
        }
    }

    /// <summary>
    /// The number of items in the span.
    /// </summary>
    public long Length {
        get => _length;
    }

    /// <summary>
    /// Returns true if Length is 0.
    /// </summary>
    public bool IsEmpty {
        get => 0 >= _length; // Workaround for https://github.com/dotnet/runtime/issues/10950
    }

    /// <summary>
    /// Returns false if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator !=(LongSpan<T> left, LongSpan<T> right) =>
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
    /// Defines an implicit conversion of an array to a <see cref="Span{T}"/>
    /// </summary>
    public static implicit operator LongSpan<T>(T[]? array) =>
        new LongSpan<T>(array);

    /// <summary>
    /// Defines an implicit conversion of a <see cref="ArraySegment{T}"/> to a <see cref="Span{T}"/>
    /// </summary>
    public static implicit operator LongSpan<T>(ArraySegment<T> segment) =>
        new LongSpan<T>(segment.Array, segment.Offset, segment.Count);

    /// <summary>
    /// Returns an empty <see cref="Span{T}"/>
    /// </summary>
    public static LongSpan<T> Empty => default;

    /// <summary>Gets an enumerator for this span.</summary>
    public Enumerator GetEnumerator() =>
        new Enumerator(this);

    /// <summary>Enumerates the elements of a <see cref="Span{T}"/>.</summary>
    public ref struct Enumerator {
        /// <summary>The span being enumerated.</summary>
        public readonly LongSpan<T> _span;

        /// <summary>The next index to yield.</summary>
        public long _index;

        /// <summary>Initialize the enumerator.</summary>
        /// <param name="span">The span to enumerate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(LongSpan<T> span) {
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
        public ref T Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    /// <summary>
    /// Returns a reference to the 0th element of the Span. If the Span is empty, returns null reference.
    /// It can be used for pinning and is required to support the use of span within a fixed statement.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref T GetPinnableReference() {
        // Ensure that the native code has just one forward branch that is predicted-not-taken.
        ref T ret = ref Unsafe.NullRef<T>();
        if (_length != 0) ret = ref _pointer.Value;
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
    public unsafe void CopyTo(LongSpan<T> destination) {
        // Using "if (!TryCopyTo(...))" results in two branches: one for the length
        // check, and one for the result of TryCopyTo. Since these checks are equivalent,
        // we can optimize by performing the check once ourselves then calling Memmove directly.

        if (_length <= destination.Length) {
            Buffer.MemoryCopy(Unsafe.AsPointer(ref _pointer.Value), Unsafe.AsPointer(ref destination._pointer.Value), _length, _length);
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
    public unsafe bool TryCopyTo(LongSpan<T> destination) {
        bool retVal = false;
        if (_length <= destination.Length) {
            Buffer.MemoryCopy(Unsafe.AsPointer(ref _pointer.Value), Unsafe.AsPointer(ref destination._pointer.Value), destination._length, _length);
            retVal = true;
        }

        return retVal;
    }

    /// <summary>
    /// Returns true if left and right point at the same memory and have the same length.  Note that
    /// this does *not* check to see if the *contents* are equal.
    /// </summary>
    public static bool operator ==(LongSpan<T> left, LongSpan<T> right) =>
        left._length == right._length &&
        Unsafe.AreSame<T>(ref left._pointer.Value, ref right._pointer.Value);

    /// <summary>
    /// For <see cref="Span{Char}"/>, returns a new instance of string that represents the characters pointed to by the span.
    /// Otherwise, returns a <see cref="string"/> with the name of the type and the number of elements.
    /// </summary>
    public override string ToString() {
        return string.Format("System.Span<{0}>[{1}]", typeof(T).Name, _length);
    }

    /// <summary>
    /// Forms a slice out of the given span, beginning at 'start'.
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe LongSpan<T> Slice(long start) {
        if (start > _length)
            throw new ArgumentOutOfRangeException();

        return new LongSpan<T>(ref Unsafe.AsRef<T>((byte*) Unsafe.AsPointer(ref _pointer.Value) + start), _length - start);
    }

    /// <summary>
    /// Forms a slice out of the given span, beginning at 'start'.
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe LongSpan<T> Slice(int start) {
        if (start > _length)
            throw new ArgumentOutOfRangeException();

        return new LongSpan<T>(ref Unsafe.AsRef<T>((byte*) Unsafe.AsPointer(ref _pointer.Value) + start), _length - start);
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
    public unsafe LongSpan<T> Slice(long start, long length) {
        if (start > _length || length > (_length - start))
            throw new ArgumentOutOfRangeException();

        return new LongSpan<T>(ref Unsafe.AsRef<T>((byte*) Unsafe.AsPointer(ref _pointer.Value) + start), length);
    }

    /// <summary>
    /// Copies the contents of this span into a new array.  This heap
    /// allocates, so should generally be avoided, however it is sometimes
    /// necessary to bridge the gap with APIs written in terms of arrays.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T[] ToArray() {
        if (_length == 0)
            return Array.Empty<T>();

        var destination = new T[_length];
        Buffer.MemoryCopy(Unsafe.AsPointer(ref _pointer.Value), Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(destination)), _length, _length);
        return destination;
    }
}