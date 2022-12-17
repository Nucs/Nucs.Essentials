using System;

namespace Nucs;

public class MarketDataException : SourceException {
    public MarketDataException(string message) : base(message) { }

    public MarketDataException(string message, Exception inner) : base(message, inner) { }
}