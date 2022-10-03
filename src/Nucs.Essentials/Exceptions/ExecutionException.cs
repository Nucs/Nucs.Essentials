using System;

namespace Nucs;

public class ExecutionException : SourceException {
    public ExecutionException(string message) : base(message) { }

    public ExecutionException(string message, Exception inner) : base(message, inner) { }
}