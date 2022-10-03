using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class ConnectionTimeoutException : Exception {
        public ConnectionTimeoutException() { }
        public ConnectionTimeoutException(string message) : base(message) { }
        public ConnectionTimeoutException(string message, Exception inner) : base(message, inner) { }

        protected ConnectionTimeoutException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}