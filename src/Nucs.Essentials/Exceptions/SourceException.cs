using System;

namespace Nucs {
    public class SourceException : Exception {
        public SourceException(string message)
            : base(message) {
        }

        public SourceException(string message, Exception inner) : base(message, inner) {
        }
    }
}