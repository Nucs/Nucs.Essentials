using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Nucs.Collections;

namespace Nucs.Essentials.Benchmarks;

/*
|                                                     Method |     Mean |     Error |    StdDev | Ratio | RatioSD |
|----------------------------------------------------------- |---------:|----------:|----------:|------:|--------:|
|                                          Channel_Unbounded | 3.747 ms | 0.1122 ms | 0.3200 ms |  1.00 |    0.00 |
|                                            Channel_Bounded | 5.779 ms | 0.1144 ms | 0.3170 ms |  1.54 |    0.12 |
|                     AsyncSingleProducerSingleConsumerQueue | 2.509 ms | 0.0489 ms | 0.0481 ms |  0.61 |    0.04 |
|          AsyncSingleProducerSingleConsumerQueue_NeverEmpty | 2.290 ms | 0.0450 ms | 0.0535 ms |  0.58 |    0.05 |
|                         AsyncManyProducerManyConsumerStack | 7.871 ms | 0.0952 ms | 0.0795 ms |  1.93 |    0.13 |
|              AsyncManyProducerManyConsumerStack_NeverEmpty | 5.354 ms | 0.1046 ms | 0.1284 ms |  1.35 |    0.11 |
|                              ManyProducerManyConsumerStack | 3.738 ms | 0.0737 ms | 0.1421 ms |  0.97 |    0.08 |
|                   ManyProducerManyConsumerStack_NeverEmpty | 4.908 ms | 0.1296 ms | 0.3739 ms |  1.32 |    0.13 |
|            SemaphoreAsyncSingleProducerSingleConsumerQueue | 5.792 ms | 0.1665 ms | 0.4830 ms |  1.55 |    0.13 |
| SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty | 6.446 ms | 0.3023 ms | 0.8819 ms |  1.73 |    0.24 |
*/
public class AsyncSingleProducerSingleConsumerQueue_EnqueueDequeue_Benchmark {
    private Channel<object> _channelUnbounded;
    private Channel<object> _channelBounded;
    private AsyncSingleProducerSingleConsumerQueue<object> _aspscq;
    private AsyncManyProducerManyConsumerStack<object> _ampmcq;
    private ManyProducerManyConsumerStack<object> _mpmcq;
    private AsyncSemaphoreSingleProducerSingleConsumerQueue<object> _saspscq;

    private const int Iterations = 100000;
    
    [IterationSetup]
    public void Setup() {
        _channelBounded = Channel.CreateBounded<object>(new BoundedChannelOptions(512) { SingleReader = true, SingleWriter = true });
        _channelUnbounded = Channel.CreateUnbounded<object>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _aspscq = new AsyncSingleProducerSingleConsumerQueue<object>(initialCapacity: 512);
        _saspscq = new AsyncSemaphoreSingleProducerSingleConsumerQueue<object>(512);
        _ampmcq = new AsyncManyProducerManyConsumerStack<object>();
        _mpmcq = new ManyProducerManyConsumerStack<object>();
    }

    [Benchmark(Baseline = true)]
    public void Channel_Unbounded() {
        var reader = _channelUnbounded.Reader;
        var writer = _channelUnbounded.Writer;
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void Channel_Bounded() {
        var reader = _channelBounded.Reader;
        var writer = _channelBounded.Writer;

        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue() {
        var ch = _aspscq;
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _aspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncManyProducerManyConsumerStack() {
        var ch = _ampmcq;
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncManyProducerManyConsumerStack_NeverEmpty() {
        var ch = _ampmcq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void ManyProducerManyConsumerStack() {
        var ch = _mpmcq;
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void ManyProducerManyConsumerStack_NeverEmpty() {
        var ch = _mpmcq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue() {
        var ch = _saspscq;
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _saspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < Iterations; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }
}