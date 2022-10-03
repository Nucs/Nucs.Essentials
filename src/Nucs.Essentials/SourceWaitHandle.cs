using System.Threading;

namespace Nucs {
    public static class SourceWaitHandle {
        public static AutoResetEvent ProcessingCompleteEvent = new AutoResetEvent(false);
    }
}