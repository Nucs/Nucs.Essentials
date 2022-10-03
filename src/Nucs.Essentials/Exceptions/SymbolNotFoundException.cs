using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class SymbolNotFoundException : Exception {
        public SymbolNotFoundException() { }
        public SymbolNotFoundException(string message) : base(message) { }
        public SymbolNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected SymbolNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}