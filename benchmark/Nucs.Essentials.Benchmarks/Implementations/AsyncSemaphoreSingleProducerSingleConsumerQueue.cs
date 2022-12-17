using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Extensions;

namespace Nucs.Collections;

/// <summary>
///     Provides a producer/consumer queue safe to be used by only one producer and one consumer concurrently.
/// </summary>
/// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
public class AsyncSemaphoreSingleProducerSingleConsumerQueue<T> : IDisposable {
    private readonly SingleProducerSingleConsumerQueue<T> _queues;
    private readonly SemaphoreSlim _notifier;

    public int Count => _notifier.CurrentCount;

    public bool IsEmpty => _notifier.CurrentCount == 0;

    public AsyncSemaphoreSingleProducerSingleConsumerQueue(int initialCapacity = 512) {
        _queues = new SingleProducerSingleConsumerQueue<T>(initialCapacity);
        _notifier = new SemaphoreSlim(0, int.MaxValue);
    }

    #region Writer

    public void Enqueue(T item) {
        _queues.Enqueue(item);
        _notifier.Release(1);
    }

    public void Enqueue(ref T item) {
        _queues.Enqueue(ref item);
        _notifier.Release(1);
    }

    #endregion

    #region Reader

    public bool TryDequeue(out T result) {
        Unsafe.SkipInit(out result);
        return _notifier.Wait(0, default) && _queues.TryDequeue(out result!);
    }

    public Task WaitForReadAsync(CancellationToken cancellationToken = default) {
        return _notifier.Wait(0, cancellationToken) ? Task.CompletedTask : _notifier.WaitAsync(-1, cancellationToken);
    }

    public Task WaitForReadAsync(int timeout, CancellationToken cancellationToken = default) {
        return _notifier.Wait(0, cancellationToken) ? Task.CompletedTask : _notifier.WaitAsync(timeout, cancellationToken);
    }

    #endregion

    public void Dispose() {
        _queues.Clear();
        while (_notifier.Wait(0)) continue;
        _notifier.TryDispose();
    }
}