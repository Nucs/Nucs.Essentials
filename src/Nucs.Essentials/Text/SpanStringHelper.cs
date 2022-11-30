using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nucs.Text;

internal static class SpanStringHelper {
    /// <summary>
    ///     Taken from ReadOnlySpan{char}.IndexOf internals.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static unsafe int IndexOf(ref char searchSpace, char value, int length) {
        nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations

        while (length >= 8) {
            length -= 8;

            if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                goto Found;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 1)))
                goto Found1;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 2)))
                goto Found2;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                goto Found3;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 4)))
                goto Found4;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 5)))
                goto Found5;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 6)))
                goto Found6;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 7)))
                goto Found7;

            index += 8;
        }

        if (length >= 4) {
            length -= 4;

            if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                goto Found;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 1)))
                goto Found1;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 2)))
                goto Found2;
            if (value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                goto Found3;

            index += 4;
        }

        while (length > 0) {
            if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                goto Found;

            index += 1;
            length--;
        }

        return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
        return (int) index;
        Found1:
        return (int) (index + 1);
        Found2:
        return (int) (index + 2);
        Found3:
        return (int) (index + 3);
        Found4:
        return (int) (index + 4);
        Found5:
        return (int) (index + 5);
        Found6:
        return (int) (index + 6);
        Found7:
        return (int) (index + 7);
    }

    /// <summary>
    ///     Taken from ReadOnlySpan{char}.IndexOf(string) internals.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int IndexOf(ref char searchSpace, int searchSpaceLength, ref char value, int valueLength) {
        Debug.Assert(searchSpaceLength >= 0);
        Debug.Assert(valueLength >= 0);

        if (valueLength == 0)
            return 0; // A zero-length sequence is always treated as "found" at the start of the search space.

        char valueHead = value;
        ref char valueTail = ref Unsafe.Add(ref value, 1);
        int valueTailLength = valueLength - 1;

        int index = 0;
        while (true) {
            Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
            int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
            if (remainingSearchSpaceLength <= 0)
                break; // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

            // Do a quick search for the first element of "value".
            int relativeIndex = IndexOf(ref Unsafe.Add(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
            if (relativeIndex < 0)
                break;
            index += relativeIndex;

            // Found the first element of "value". See if the tail matches.
            if (SequenceEqual(ref Unsafe.Add(ref searchSpace, index + 1), ref valueTail, valueTailLength))
                return index; // The tail matched. Return a successful find.

            index++;
        }

        return -1;
    }

    /// <summary>
    ///     Taken from ReadOnlySpan{char}.IndexOf(string) internals.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool SequenceEqual(ref char first, ref char second, int length) {
        Debug.Assert(length >= 0);

        if (Unsafe.AreSame(ref first, ref second))
            goto Equal;

        nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
        char lookUp0;
        char lookUp1;
        while (length >= 8) {
            length -= 8;

            lookUp0 = Unsafe.Add(ref first, index);
            lookUp1 = Unsafe.Add(ref second, index);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 1);
            lookUp1 = Unsafe.Add(ref second, index + 1);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 2);
            lookUp1 = Unsafe.Add(ref second, index + 2);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 3);
            lookUp1 = Unsafe.Add(ref second, index + 3);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 4);
            lookUp1 = Unsafe.Add(ref second, index + 4);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 5);
            lookUp1 = Unsafe.Add(ref second, index + 5);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 6);
            lookUp1 = Unsafe.Add(ref second, index + 6);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 7);
            lookUp1 = Unsafe.Add(ref second, index + 7);
            if (lookUp0 != lookUp1)
                goto NotEqual;

            index += 8;
        }

        if (length >= 4) {
            length -= 4;

            lookUp0 = Unsafe.Add(ref first, index);
            lookUp1 = Unsafe.Add(ref second, index);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 1);
            lookUp1 = Unsafe.Add(ref second, index + 1);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 2);
            lookUp1 = Unsafe.Add(ref second, index + 2);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            lookUp0 = Unsafe.Add(ref first, index + 3);
            lookUp1 = Unsafe.Add(ref second, index + 3);
            if (lookUp0 != lookUp1)
                goto NotEqual;

            index += 4;
        }

        while (length > 0) {
            lookUp0 = Unsafe.Add(ref first, index);
            lookUp1 = Unsafe.Add(ref second, index);
            if (lookUp0 != lookUp1)
                goto NotEqual;
            index += 1;
            length--;
        }

        Equal:
        return true;

        NotEqual: // Workaround for https://github.com/dotnet/runtime/issues/8795
        return false;
    }
}