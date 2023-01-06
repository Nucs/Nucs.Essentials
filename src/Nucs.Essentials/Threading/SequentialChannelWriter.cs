using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nucs.Threading {
    public class SequentialChannelWriter<T> : ChannelWriter<T> where T : unmanaged {
        #if NET6_0_OR_GREATER
        private static readonly ValueTask<bool> s_true = ValueTask.FromResult(true);
        #endif
        public delegate void Handler(ref T obj);

        public readonly Handler Destinition;

        public SequentialChannelWriter(Handler destinition) {
            Destinition = destinition;
        }

        public override bool TryWrite(T item) {
            Destinition(ref item);
            return true;
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = new CancellationToken()) {
            #if !NET6_0_OR_GREATER
            return new ValueTask<bool>(true);
            #else
            return s_true;
            #endif
        }
    }
}