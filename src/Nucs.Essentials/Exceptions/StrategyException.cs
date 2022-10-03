using System;

namespace Nucs;

public class StrategyException : SourceException {
    public StrategyException(string message) : base(message) { }

    public StrategyException(string message, Exception inner) : base(message, inner) { }
}