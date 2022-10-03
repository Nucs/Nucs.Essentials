using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class NanexException : Exception {
        public NanexException() { }
        public NanexException(string message) : base(message) { }
        public NanexException(string message, Exception inner) : base(message, inner) { }

        protected NanexException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}