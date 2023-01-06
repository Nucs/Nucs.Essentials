#if NET6_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading.Tasks;

namespace Nucs.Collections;

/// <summary>
///     Provides a producer/consumer queue safe to be used by only one producer and one consumer concurrently, allowing reader to wait for new items asyncronously.
/// </summary>
/// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
public class AsyncSingleProducerSingleConsumerQueue<T> : IDisposable {
    private readonly SingleProducerSingleConsumerQueue<T> _queues;
    private readonly ValueTaskCompletionSource _notifier;
    private volatile int _count;
    private volatile int _notificationWaiters;

    public int Count => _count;

    public bool IsEmpty => _count == 0;

    public AsyncSingleProducerSingleConsumerQueue(int initialCapacity = 512, bool runContinuationsAsynchronously = true) {
        _queues = new SingleProducerSingleConsumerQueue<T>(initialCapacity);
        _notifier = new ValueTaskCompletionSource(runContinuationsAsynchronously);
    }

    #region Writer

    public void Enqueue(T item) {
        _queues.Enqueue(item);
        if (Interlocked.Increment(ref _count) == 1 && _notificationWaiters > 0)
            _notifier.TrySetResult();
    }

    public void Enqueue(ref T item) {
        _queues.Enqueue(ref item);
        if (Interlocked.Increment(ref _count) == 1 && _notificationWaiters > 0)
            _notifier.TrySetResult();
    }

    #endregion

    #region Reader

    public bool TryDequeue(out T result) {
        if (!_queues.TryDequeue(out result))
            return false;

        if (Interlocked.Decrement(ref _count) == 0 && _notificationWaiters > 0)
            _notifier.Reset();

        return true;
    }

    public async ValueTask WaitForReadAsync(CancellationToken cancellationToken = default) {
        if (_count > 0)
            return; //completed

        Interlocked.Increment(ref _notificationWaiters);
        await _notifier.CreateTask(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        Interlocked.Decrement(ref _notificationWaiters);
    }

    public async ValueTask WaitForReadAsync(TimeSpan timeout, CancellationToken cancellationToken = default) {
        if (_count > 0)
            return; //completed

        Interlocked.Increment(ref _notificationWaiters);
        await _notifier.CreateTask(timeout, cancellationToken).ConfigureAwait(false);
        Interlocked.Decrement(ref _notificationWaiters);
    }

    #endregion

    public void Dispose() {
        _notifier.TrySetCanceled(default);
        _queues.Clear();
    }
}
#endif