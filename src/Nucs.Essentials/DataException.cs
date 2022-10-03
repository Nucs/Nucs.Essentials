using System;

namespace Nucs;

public class DataException : SourceException {
    public DataException(string message) : base(message) { }

    public DataException(string message, Exception inner) : base(message, inner) { }
}