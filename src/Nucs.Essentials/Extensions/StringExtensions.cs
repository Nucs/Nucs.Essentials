using System;
using System.Diagnostics;
using System.Numerics;

namespace Nucs.Extensions {
    public static class StringExtensions {
        public static LineReader SplitReader(this string str) => new LineReader(str);

        public static LineReader SplitReader(this ReadOnlySpan<char> str) => new LineReader(str);

        public static unsafe int GetStableHashCode(this string str) {
            unchecked {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;
                fixed (char* chars = str) {
                    for (int i = 0; i < str.Length; i += 2) {
                        hash1 = ((hash1 << 5) + hash1) ^ chars[i];
                        if (i == str.Length - 1)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ chars[i + 1];
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
        /// or are otherwise mitigated
        /// this is the internal method used in .NET 6
        public static unsafe int GetNonRandomizedHashCode(this string str) {
            fixed (char* src = str) {
                Debug.Assert(src[str.Length] == '\0', "src[this.Length] == '\\0'");
                Debug.Assert(((int) src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptr = (uint*) src;
                int length = str.Length;

                while (length > 2) {
                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                if (length > 0) {
                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
                }

                return (int) (hash1 + (hash2 * 1566083941));
            }
        }

        /// Use this if and only if 'Denial of Service' attacks are not a concern (i.e. never used for free-form user input),
        /// or are otherwise mitigated
        /// this is the internal method used in .NET 6
        public static unsafe int GetNonRandomizedHashCode(this ReadOnlySpan<char> str) {
            fixed (char* src = str) {
                Debug.Assert(src[str.Length] == '\0', "src[this.Length] == '\\0'");
                Debug.Assert(((int) src) % 4 == 0, "Managed string should start at 4 bytes boundary");

                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptr = (uint*) src;
                int length = str.Length;

                while (length > 2) {
                    length -= 4;
                    // Where length is 4n-1 (e.g. 3,7,11,15,19) this additionally consumes the null terminator
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                    ptr += 2;
                }

                if (length > 0) {
                    // Where length is 4n-3 (e.g. 1,5,9,13,17) this additionally consumes the null terminator
                    hash2 = (BitOperations.RotateLeft(hash2, 5) + hash2) ^ ptr[0];
                }

                return (int) (hash1 + (hash2 * 1566083941));
            }
        }

        public static unsafe int GetNonRandomizedHashCodeOrdinalIgnoreCase(this string str) {
            return GetNonRandomizedHashCodeOrdinalIgnoreCase(str.AsSpan());
        }

        public static unsafe int GetNonRandomizedHashCodeOrdinalIgnoreCase(this ReadOnlySpan<char> str) {
            Span<char> span = stackalloc char[str.Length];
            str.ToLowerInvariant(span);
            return GetNonRandomizedHashCode(span);
        }

        /// <summary>
        ///     Cvs like column appending
        /// </summary>
        /// <param name="text">Destinition</param>
        /// <param name="addition"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string AppendString(this string? text, string addition, string delimiter) {
            if (string.IsNullOrEmpty(addition)) {
                return text ?? addition;
            }

            if (string.IsNullOrEmpty(text)) {
                return addition;
            }

            return text + delimiter + addition;
        }
    }
}