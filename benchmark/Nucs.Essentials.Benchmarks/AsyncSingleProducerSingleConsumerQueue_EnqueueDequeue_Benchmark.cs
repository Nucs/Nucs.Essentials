using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Nucs.Collections;

namespace Nucs.Essentials.Benchmarks;

/*
|                                                     Method |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |
|----------------------------------------------------------- |---------:|----------:|----------:|---------:|------:|--------:|
|                                          Channel_Unbounded | 6.411 ms | 0.1827 ms | 0.5301 ms | 6.364 ms |  1.00 |    0.00 |
|                                            Channel_Bounded | 7.288 ms | 0.2194 ms | 0.6189 ms | 7.227 ms |  1.14 |    0.13 |
|                     AsyncSingleProducerSingleConsumerQueue | 3.551 ms | 0.1406 ms | 0.4034 ms | 3.440 ms |  0.55 |    0.07 |
|          AsyncSingleProducerSingleConsumerQueue_NeverEmpty | 3.081 ms | 0.0938 ms | 0.2630 ms | 3.117 ms |  0.48 |    0.04 |
|            SemaphoreAsyncSingleProducerSingleConsumerQueue | 8.654 ms | 0.5181 ms | 1.4948 ms | 8.044 ms |  1.35 |    0.22 |
| SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty | 8.236 ms | 0.1961 ms | 0.5720 ms | 8.168 ms |  1.29 |    0.13 |
 */
public class AsyncSingleProducerSingleConsumerQueue_EnqueueDequeue_Benchmark {
    private Channel<object> _channelUnbounded;
    private Channel<object> _channelBounded;
    private AsyncSingleProducerSingleConsumerQueue<object> _aspscq;
    private AsyncSemaphoreSingleProducerSingleConsumerQueue<object> _saspscq;

    [IterationSetup]
    public void Setup() {
        _channelBounded = Channel.CreateBounded<object>(new BoundedChannelOptions(512 * 256) { SingleReader = true, SingleWriter = true });
        _channelUnbounded = Channel.CreateUnbounded<object>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        _aspscq = new AsyncSingleProducerSingleConsumerQueue<object>(initialCapacity: 512);
        _saspscq = new AsyncSemaphoreSingleProducerSingleConsumerQueue<object>(512);
    }

    [Benchmark(Baseline = true)]
    public void Channel_Unbounded() {
        var reader = _channelBounded.Reader;
        var writer = _channelUnbounded.Writer;
        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void Channel_Bounded() {
        var reader = _channelBounded.Reader;
        var writer = _channelBounded.Writer;

        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            writer.TryWrite(n);
            reader.TryRead(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue() {
        var ch = _aspscq;
        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void AsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _aspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue() {
        var ch = _saspscq;
        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }

    [Benchmark]
    public void SemaphoreAsyncSingleProducerSingleConsumerQueue_NeverEmpty() {
        var ch = _saspscq;
        ch.Enqueue(new object()); //by having additional item, the queue is never empty and doesnt need to signal the notifier.
        object n = new object();
        for (int i = 0; i < 512 * 256; i++) {
            ch.Enqueue(n);
            ch.TryDequeue(out n);
        }
    }
}