using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Nucs.Threading {
    public class SequentialChannelWriter<T> : ChannelWriter<T> where T : unmanaged {
        private static readonly ValueTask<bool> s_true = ValueTask.FromResult(true);
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
            return s_true;
        }
    }
}