using System;

namespace Nucs.Extensions;

public enum LineDelimiter {
    /// <summary>
    ///     \r
    /// </summary>
    CR,

    /// <summary>
    ///     \n
    /// </summary>
    LF,

    /// <summary>
    ///     \r\n
    /// </summary>
    CRLF
}

public static class LineDelimiterExtensions {
    /// <summary>
    ///     Returns the string representation of the delimiter.
    /// </summary>
    public static string AsString(this LineDelimiter delimiter) {
        switch (delimiter) {
            case LineDelimiter.CR:
                return "\r";
            case LineDelimiter.LF:
                return "\n";
            case LineDelimiter.CRLF:
                return "\r";
            default: throw new ArgumentOutOfRangeException(nameof(delimiter), delimiter, null);
        }
    }
}