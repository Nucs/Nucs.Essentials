// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Nucs.Text;

internal ref struct ValueStringBuilderDebugView {
    public ValueStringBuilderDebugView(ValueStringBuilder sb) : this() {
        Value = sb.ToString();
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string Value;
}

/// <summary>
///     A value <see cref="StringBuilder"/> taken from internals of .NET 7. Rents array buffer from <see cref="ArrayPool{T}.Shared"/> which supports resizable by bucket indexing.
/// </summary>
[DebuggerTypeProxy(typeof(ValueStringBuilderDebugView))]
[DebuggerDisplay("{ToString(),raw}")]
public ref struct ValueStringBuilder {
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private char[]? _arrayToReturnToPool;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Span<char> _chars;

    /// <summary>
    ///     The number of characters present in the <see cref="ValueStringBuilder"/>.
    /// </summary>
    private int _length;

    public ValueStringBuilder(Span<char> initialBuffer) {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _length = 0;
    }

    public ValueStringBuilder(Span<char> initialBuffer, int startIndex) {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _length = startIndex;
    }

    public ValueStringBuilder(int initialCapacity) {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool.AsSpan();
        _length = 0;
    }

    public int Length {
        get => _length;
        set {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _chars.Length);
            _length = value;
        }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity) {
        if (capacity > _chars.Length)
            Grow(capacity - _length);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference() {
        return ref MemoryMarshal.GetReference(_chars);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate) {
        if (terminate) {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index] {
        get {
            Debug.Assert(index < _length);
            return ref _chars[index];
        }
    }

    public override string ToString() {
        if (_length == 0)
            return string.Empty;

        return _chars.Slice(0, _length).ToString();
    }

    public string ToStringFinalize() {
        string s = _chars.Slice(0, _length).ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate) {
        if (terminate) {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return _chars.Slice(0, _length);
    }

    public ReadOnlySpan<char> AsSpan() =>
        _chars.Slice(0, _length);

    public ReadOnlySpan<char> AsSpan(int start) =>
        _chars.Slice(start, _length - start);

    public ReadOnlySpan<char> AsSpan(int start, int length) =>
        _chars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten) {
        if (_chars.Slice(0, _length).TryCopyTo(destination)) {
            charsWritten = _length;
            Dispose();
            return true;
        } else {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count) {
        if (_length > _chars.Length - count) {
            Grow(count);
        }

        int remaining = _length - index;
        _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
        _chars.Slice(index, count).Fill(value);
        _length += count;
    }

    public void Insert(int index, string? s) {
        if (s == null) {
            return;
        }

        int count = s.Length;

        if (_length > (_chars.Length - count)) {
            Grow(count);
        }

        int remaining = _length - index;
        _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
        s.AsSpan().CopyTo(_chars.Slice(index));
        _length += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c) {
        int pos = _length;
        if ((uint) pos < (uint) _chars.Length) {
            _chars[pos] = c;
            _length = pos + 1;
        } else {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s) {
        if (s == null) {
            return;
        }

        int pos = _length;
        if (s.Length == 1 && (uint) pos < (uint) _chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _length = pos + 1;
        } else {
            AppendSlow(s);
        }
    }

    private void AppendSlow(string s) {
        int pos = _length;
        if (pos > _chars.Length - s.Length) {
            Grow(s.Length);
        }

        s.AsSpan().CopyTo(_chars.Slice(pos));
        _length += s.Length;
    }

    public void Append(char c, int count) {
        if (_length > _chars.Length - count) {
            Grow(count);
        }

        Span<char> dst = _chars.Slice(_length, count);
        for (int i = 0; i < dst.Length; i++) {
            dst[i] = c;
        }

        _length += count;
    }

    public unsafe void Append(char* value, int length) {
        int pos = _length;
        if (pos > _chars.Length - length) {
            Grow(length);
        }

        Span<char> dst = _chars.Slice(_length, length);
        for (int i = 0; i < dst.Length; i++) {
            dst[i] = *value++;
        }

        _length += length;
    }

    public unsafe int Append(Span<byte> encodedString, Encoding encoding) {
        //decode
        Span<char> decodedString = stackalloc char[encoding.GetMaxCharCount(encodedString.Length)];
        var length = encoding.GetChars(bytes: encodedString, chars: decodedString);

        //ensure capacity with decodedString's length
        int pos = _length;
        if (pos > _chars.Length - length) {
            Grow(length);
        }

        //append by copying
        decodedString.Slice(0, length /*excludes \0 at end*/).CopyTo(_chars.Slice(pos));
        _length += length;
        return length; //return the written span
    }

    public void RemoveStart(int length) {
        length = Math.Min(length, _length);
        _chars.Slice(length, _length - length)
              .CopyTo(_chars);
        _length -= length;
    }

    public void Clear() {
        _length = 0;
    }

    public Span<char> Append(ReadOnlySpan<char> value) {
        int currentLength = _length;
        if (currentLength > _chars.Length - value.Length) {
            Grow(value.Length);
        }

        var dst = _chars.Slice(_length, value.Length);
        value.CopyTo(dst);
        _length += value.Length;
        return dst;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length) {
        int origPos = _length;
        if (origPos > _chars.Length - length) {
            Grow(length);
        }

        _length = origPos + length;
        return _chars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c) {
        Grow(1);
        Append(c);
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="_length"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos) {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(_length > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

        char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_length + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars.Slice(0, _length).CopyTo(poolArray);

        char[]? toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null) {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="_length"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndDisposeLength(int additionalCapacityBeyondPos, int disposableLength) {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(_length > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

        char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_length + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars.Slice(0, _length).CopyTo(poolArray);

        char[]? toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null) {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() {
        char[]? toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null) {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}