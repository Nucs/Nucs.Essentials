using System;
using System.Runtime.Serialization;

namespace Nucs.Exceptions {
    [Serializable]
    public class ModuleIncompatibleException : Exception {
        public ModuleIncompatibleException() { }
        public ModuleIncompatibleException(string message) : base(message) { }
        public ModuleIncompatibleException(string message, Exception inner) : base(message, inner) { }

        protected ModuleIncompatibleException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}