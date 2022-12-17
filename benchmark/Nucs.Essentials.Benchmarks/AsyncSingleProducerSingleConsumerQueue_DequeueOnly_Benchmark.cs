using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Nucs.Collections;
using Nucs.Threading;

namespace Nucs.Essentials.Benchmarks;

/*
|                                          Method |     Mean |     Error |    StdDev | Ratio | RatioSD |
|------------------------------------------------ |---------:|----------:|----------:|------:|--------:|
|                               Channel_Unbounded | 3.367 ms | 0.0665 ms | 0.1516 ms |  1.00 |    0.00 |
|                                 Channel_Bounded | 3.602 ms | 0.0719 ms | 0.1830 ms |  1.06 |    0.07 |
|          AsyncSingleProducerSingleConsumerQueue | 1.273 ms | 0.0131 ms | 0.0122 ms |  0.38 |    0.02 |
| SemaphoreAsyncSingleProducerSingleConsumerQueue | 3.174 ms | 0.0608 ms | 0.0650 ms |  0.95 |    0.05 |
 */
public class AsyncSingleProducerSingleConsumerQueue_DequeueOnly_Benchmark {
    private Channel<object> _readerUnbounded;
    private Channel<object> _readerBounded;
    private AsyncSingleProducerSingleConsumerQueue<object> _aspscq;
    private AsyncSemaphoreSingleProducerSingleConsumerQueue<object> _saspscq;

    [IterationSetup]
    public void Setup() {
        _readerBounded = Channel.CreateBounded<object>(new BoundedChannelOptions(512 * 256) { SingleReader = true, SingleWriter = true });
        _readerUnbounded = Channel.CreateUnbounded<object>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _aspscq = new AsyncSingleProducerSingleConsumerQueue<object>();
        _saspscq = new AsyncSemaphoreSingleProducerSingleConsumerQueue<object>();

        for (int j = 0; j < 256; j++) {
            for (int i = 0; i < 512; i++) {
                _aspscq.Enqueue(new object());
                _readerUnbounded.Writer.TryWrite(new object());
                _readerBounded.Writer.TryWrite(new object());
                _saspscq.Enqueue(new object());
            }
        }
    }

    [Benchmark(Baseline = true)]
    public void Channel_Unbounded() {
        var reader = _readerBounded.Reader;
        object n;
        for (int i = 0; i < 512 * 256; i++) {
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void Channel_Bounded() {
        var reader = _readerBounded.Reader;
        object n;
        for (int i = 0; i < 512 * 256; i++) {
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue() {
        var ch = _aspscq;
        object n;
        for (int i = 0; i < 512 * 256; i++) {
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue() {
        var ch = _saspscq;
        object n;
        for (int i = 0; i < 512 * 256; i++) {
            ch.TryDequeue(out n);
        }
    }
}