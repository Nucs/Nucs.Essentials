using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class MathException : Exception {
        public MathException() { }
        public MathException(string message) : base(message) { }
        public MathException(string message, Exception inner) : base(message, inner) { }

        protected MathException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}