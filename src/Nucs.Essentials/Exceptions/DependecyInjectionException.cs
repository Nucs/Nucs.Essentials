using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class DependecyInjectionException : Exception {
        public DependecyInjectionException() { }
        public DependecyInjectionException(string message) : base(message) { }
        public DependecyInjectionException(string message, Exception inner) : base(message, inner) { }

        protected DependecyInjectionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}