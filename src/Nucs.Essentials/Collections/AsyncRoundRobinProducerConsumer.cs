#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading;
using Nucs.Collections;
using Nucs.Collections.Structs;

namespace Nucs;

/// <summary>
///     A thread-safe consumer that feeds N <see cref="AsyncSingleProducerSingleConsumerQueue{T}"/> queues in a round-robin fashion.
///     Supports adding and removing consumers on the fly.
/// </summary>
public class AsyncRoundRobinProducerConsumer<T> {
    private StructList<AsyncSingleProducerSingleConsumerQueue<T>> _consumers;
    private uint _pointer;

    public AsyncRoundRobinProducerConsumer() {
        _consumers = new StructList<AsyncSingleProducerSingleConsumerQueue<T>>(8);
    }

    public AsyncRoundRobinProducerConsumer(StructList<AsyncSingleProducerSingleConsumerQueue<T>> consumers) {
        _consumers = consumers;
        _pointer = (uint) consumers.Length;
        for (int i = 0; i < consumers.Length; i++) {
            if (consumers[i] == null)
                throw new NullReferenceException();
        }
    }

    public AsyncRoundRobinProducerConsumer(IEnumerable<AsyncSingleProducerSingleConsumerQueue<T>> consumers) {
        _consumers = new StructList<AsyncSingleProducerSingleConsumerQueue<T>>(8);
        foreach (var consumerQueue in consumers) {
            _consumers.Add(consumerQueue);
        }

        _pointer = (uint) _consumers.Length;
    }

    #region Writer

    public void Enqueue(T item) {
        _consumers[unchecked(Interlocked.Increment(ref _pointer)) % _consumers._count]
           .Enqueue(item);
    }

    public void Enqueue(ref T item) {
        _consumers[unchecked(Interlocked.Increment(ref _pointer)) % _consumers._count]
           .Enqueue(ref item);
    }

    public void EnqueueRange<TEnumerable>(TEnumerable items) where TEnumerable : IEnumerable<T> {
        var (arr, cnt) = _consumers;
        foreach (var item in items) {
            arr[unchecked(Interlocked.Increment(ref _pointer)) % cnt]
               .Enqueue(item);
        }
    }

    public void Reset() {
        Interlocked.Exchange(ref _pointer, 0);
    }

    #endregion

    #region Reader

    public void AddConsumer(AsyncSingleProducerSingleConsumerQueue<T> consumer) {
        lock (this)
            _consumers.Add(consumer);

        if (_pointer < _consumers._count)
            Interlocked.Increment(ref _pointer); //ensures 0 index is always used first.
    }

    public void RemoveConsumer(AsyncSingleProducerSingleConsumerQueue<T> consumer) {
        bool success;
        lock (this) {
            success = _consumers.Remove(consumer, ReferenceEquals);
        }

        if (success) {
            Interlocked.Decrement(ref _pointer);
        }
    }

    public AsyncSingleProducerSingleConsumerQueue<T> AddConsumer() {
        var consumer = new AsyncSingleProducerSingleConsumerQueue<T>();
        AddConsumer(consumer);
        return consumer;
    }

    #endregion

    public void Dispose() {
        var (arr, count) = _consumers;
        for (var i = 0; i < count; i++) {
            arr[i].Dispose();
        }

        _consumers.Dispose();
    }
}
#endif