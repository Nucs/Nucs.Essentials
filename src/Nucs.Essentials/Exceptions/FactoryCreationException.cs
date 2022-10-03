using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class FactoryCreationException : Exception {
        public FactoryCreationException() { }
        public FactoryCreationException(string message) : base(message) { }
        public FactoryCreationException(string message, Exception inner) : base(message, inner) { }

        protected FactoryCreationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}