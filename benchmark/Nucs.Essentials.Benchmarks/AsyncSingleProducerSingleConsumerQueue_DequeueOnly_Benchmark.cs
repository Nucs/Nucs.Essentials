using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Nucs.Collections;
using Nucs.Threading;

namespace Nucs.Essentials.Benchmarks;

/*
|                                          Method |       Mean |     Error |    StdDev | Ratio | RatioSD |
|------------------------------------------------ |-----------:|----------:|----------:|------:|--------:|
|                               Channel_Unbounded | 2,545.0 us |  49.76 us |  74.48 us |  1.00 |    0.00 |
|                                 Channel_Bounded | 2,491.0 us |  34.44 us |  28.76 us |  0.97 |    0.03 |
|          AsyncSingleProducerSingleConsumerQueue |   964.3 us |  11.40 us |  14.00 us |  0.38 |    0.01 |
| SemaphoreAsyncSingleProducerSingleConsumerQueue | 3,030.1 us | 189.97 us | 557.16 us |  1.36 |    0.19 |
|                   ManyProducerManyConsumerStack | 1,332.5 us |  35.11 us |  99.61 us |  0.54 |    0.05 |
 */
public class AsyncSingleProducerSingleConsumerQueue_DequeueOnly_Benchmark {
    private Channel<object> _readerUnbounded;
    private Channel<object> _readerBounded;
    private AsyncSingleProducerSingleConsumerQueue<object> _aspscq;
    private AsyncSemaphoreSingleProducerSingleConsumerQueue<object> _saspscq;
    private ManyProducerManyConsumerStack<object> _mpmcs;
    
    private const int Iterations = 100000;

    [IterationSetup]
    public void Setup() {
        _readerBounded = Channel.CreateBounded<object>(new BoundedChannelOptions(Iterations) { SingleReader = true, SingleWriter = true });
        _readerUnbounded = Channel.CreateUnbounded<object>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _aspscq = new AsyncSingleProducerSingleConsumerQueue<object>();
        _saspscq = new AsyncSemaphoreSingleProducerSingleConsumerQueue<object>();
        _mpmcs = new ManyProducerManyConsumerStack<object>();
        var item = new object();
        for (int j = 0; j < Iterations; j++) {
            _aspscq.Enqueue(item);
            _readerUnbounded.Writer.TryWrite(item);
            _readerBounded.Writer.TryWrite(item);
            _saspscq.Enqueue(item);
            _mpmcs.Enqueue(item);
        }
    }

    [Benchmark(Baseline = true)]
    public void Channel_Unbounded() {
        var reader = _readerBounded.Reader;
        object n;
        for (int i = 0; i < Iterations; i++) {
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void Channel_Bounded() {
        var reader = _readerBounded.Reader;
        object n;
        for (int i = 0; i < Iterations; i++) {
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue() {
        var ch = _aspscq;
        object n;
        for (int i = 0; i < Iterations; i++) {
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue() {
        var ch = _saspscq;
        object n;
        for (int i = 0; i < Iterations; i++) {
            ch.TryDequeue(out n);
        }
    }
    [Benchmark]
    public void ManyProducerManyConsumerStack() {
        var ch = _mpmcs;
        object n;
        for (int i = 0; i < Iterations; i++) {
            ch.TryDequeue(out n);
        }
    }
}