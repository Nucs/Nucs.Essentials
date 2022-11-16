using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

namespace Nucs.Extensions;

//internalized after benchmark proven nonefficient
internal static class ASCIIEncodingHelper {
    /// <summary>
    ///     Converts a bytes span to an ASCII char span.
    /// </summary>
    public static unsafe string DecodeToString(ReadOnlySpan<byte> source) {
        var len = source.Length;
        var str = new string('\0', len); //fast allocation internally

        fixed (byte* src = source) {
            fixed (char* dstChars = str) {
                byte* dst = (byte*) dstChars;
                for (int i = 0, j = 0; i < len; i++, j += 2) {
                    dst[j] = src[i];
                }

                return str;
            }
        }
    }

    /// <summary>
    ///     Converts a bytes span to an ASCII char span.
    /// </summary>
    public static unsafe void Decode(ReadOnlySpan<byte> source, Span<char> dest) {
        //ensure no overflow
        var len = source.Length;
        if (len < dest.Length)
            throw new ArgumentException("Source is larger than destination");

        fixed (byte* src = source) {
            fixed (void* dstChars = dest) {
                byte* dst = (byte*) dstChars;
                for (int i = 0, j = 0; i < len; i++, j += 2) {
                    dst[j] = src[i];
                }
            }
        }
    }

    /// <summary>
    ///     Converts a bytes span to an ASCII char span.
    /// </summary>
    public static unsafe void Encode(ReadOnlySpan<char> source, Span<byte> dest) {
        //ensure no overflow
        var len = source.Length;
        if (dest.Length < len)
            throw new ArgumentException("Source is larger than destination");

        fixed (void* srcChars = source) {
            fixed (byte* dst = dest) {
                byte* src = (byte*) srcChars;
                for (int i = 0, j = 0; i < len; i++, j += 2) {
                    dst[i] = src[j];
                }
            }
        }
    }

    /// <summary>
    ///     Converts a bytes span to an ASCII char span.
    /// </summary>
    public static unsafe void Decode(ReadOnlySpan<byte> source, ReadOnlySpan<char> dest) {
        //ensure no overflow
        var len = source.Length;
        if (len < dest.Length)
            throw new ArgumentException("Source is larger than destination");

        fixed (byte* src = source) {
            fixed (void* dstChars = dest) {
                byte* dst = (byte*) dstChars;
                for (int i = 0, j = 0; i < len; i++, j += 2) {
                    dst[j] = src[i];
                }
            }
        }
    }

    /// <summary>
    ///     Converts a bytes span to an ASCII char span.
    /// </summary>
    public static unsafe void Encode(ReadOnlySpan<char> source, ReadOnlySpan<byte> dest) {
        //ensure no overflow
        var len = source.Length;
        if (dest.Length < len)
            throw new ArgumentException("Source is larger than destination");

        fixed (void* srcChars = source) {
            fixed (byte* dst = dest) {
                byte* src = (byte*) srcChars;
                for (int i = 0, j = 0; i < len; i++, j += 2) {
                    dst[i] = src[j];
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetByteCount(string source) {
        return source.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetByteCount(ReadOnlySpan<char> source) {
        return source.Length;
    }
}