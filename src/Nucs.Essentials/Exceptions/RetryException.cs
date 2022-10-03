using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class RetryException : Exception {
        public RetryException() { }
        public RetryException(string message) : base(message) { }
        public RetryException(string message, Exception inner) : base(message, inner) { }
        public RetryException(Exception inner) : base(string.Empty, inner) { }

        protected RetryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AbortRetryException : Exception {
        public AbortRetryException() { }
        public AbortRetryException(string message) : base(message) { }
        public AbortRetryException(string message, Exception inner) : base(message, inner) { }

        protected AbortRetryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AbortRetryAndThrowException : Exception {
        public AbortRetryAndThrowException() { }
        public AbortRetryAndThrowException(string message) : base(message) { }
        public AbortRetryAndThrowException(string message, Exception inner) : base(message, inner) { }
        public AbortRetryAndThrowException(Exception inner) : base("Operation aborted due to inner exception.", inner) { }

        protected AbortRetryAndThrowException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}