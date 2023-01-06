using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Nucs.Collections;

namespace Nucs.Essentials.Benchmarks;

/*
|                                                     Method |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |
|----------------------------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|
|                                          Channel_Unbounded | 11.305 ms | 0.2222 ms | 0.4437 ms | 11.113 ms |  1.00 |    0.00 |
|                                            Channel_Bounded | 12.915 ms | 0.2420 ms | 0.2146 ms | 12.901 ms |  1.09 |    0.04 |
|                     AsyncSingleProducerSingleConsumerQueue |  6.484 ms | 0.0274 ms | 0.0229 ms |  6.477 ms |  0.55 |    0.02 |
|          AsyncSingleProducerSingleConsumerQueue_NeverEmpty |  5.949 ms | 0.0491 ms | 0.0383 ms |  5.961 ms |  0.50 |    0.01 |
|                         AsyncManyProducerManyConsumerStack | 11.718 ms | 0.3348 ms | 0.9713 ms | 11.830 ms |  1.10 |    0.05 |
|              AsyncManyProducerManyConsumerStack_NeverEmpty | 10.409 ms | 0.1806 ms | 0.4113 ms | 10.339 ms |  0.92 |    0.04 |
|                              ManyProducerManyConsumerStack | 11.349 ms | 0.2087 ms | 0.1850 ms | 11.344 ms |  0.96 |    0.04 |
|                   ManyProducerManyConsumerStack_NeverEmpty |  9.782 ms | 0.0774 ms | 0.1597 ms |  9.748 ms |  0.87 |    0.03 |
|            SemaphoreAsyncSingleProducerSingleConsumerQueue | 13.456 ms | 0.2347 ms | 0.4048 ms | 13.309 ms |  1.19 |    0.05 |
| SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty | 12.770 ms | 0.0726 ms | 0.1432 ms | 12.747 ms |  1.13 |    0.05 |*/
public class AsyncSingleProducerSingleConsumerQueue_EnqueueDequeue_Benchmark {
    private Channel<object> _channelUnbounded;
    private Channel<object> _channelBounded;
    private AsyncSingleProducerSingleConsumerQueue<object> _aspscq;
    private AsyncManyProducerManyConsumerStack<object> _ampmcq;
    private ManyProducerManyConsumerStack<object> _mpmcq;
    private AsyncSemaphoreSingleProducerSingleConsumerQueue<object> _saspscq;

    [IterationSetup]
    public void Setup() {
        _channelBounded = Channel.CreateBounded<object>(new BoundedChannelOptions(512 * 512) { SingleReader = true, SingleWriter = true });
        _channelUnbounded = Channel.CreateUnbounded<object>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _aspscq = new AsyncSingleProducerSingleConsumerQueue<object>(initialCapacity: 512);
        _saspscq = new AsyncSemaphoreSingleProducerSingleConsumerQueue<object>(512);
        _ampmcq = new AsyncManyProducerManyConsumerStack<object>();
        _mpmcq = new ManyProducerManyConsumerStack<object>();
    }

    [Benchmark(Baseline = true)]
    public void Channel_Unbounded() {
        var reader = _channelBounded.Reader;
        var writer = _channelUnbounded.Writer;
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void Channel_Bounded() {
        var reader = _channelBounded.Reader;
        var writer = _channelBounded.Writer;

        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue() {
        var ch = _aspscq;
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _aspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncManyProducerManyConsumerStack() {
        var ch = _ampmcq;
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncManyProducerManyConsumerStack_NeverEmpty() {
        var ch = _ampmcq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void ManyProducerManyConsumerStack() {
        var ch = _mpmcq;
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void ManyProducerManyConsumerStack_NeverEmpty() {
        var ch = _mpmcq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue() {
        var ch = _saspscq;
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _saspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 512; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }
}