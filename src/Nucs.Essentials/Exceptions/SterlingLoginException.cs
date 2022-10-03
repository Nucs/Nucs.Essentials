using System;

namespace Nucs.Exceptions {
    [Serializable]
    public class SterlingLoginException : Exception {
        public SterlingLoginException() { }
        public SterlingLoginException(string message) : base(message) { }
        public SterlingLoginException(string message, Exception innerException) : base(message, innerException) { }
    }
}