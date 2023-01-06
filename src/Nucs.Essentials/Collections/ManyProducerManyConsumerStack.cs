using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Nucs.Collections;

/// <summary>
///     An collection FIFO of unique elements that can be accessed by multiple threads concurrently for both read and write.
/// </summary>
public class ManyProducerManyConsumerStack<T> : IDisposable {
    private sealed class Node {
        public readonly T Value;
        public Node? Next;

        public Node(ref T value) {
            Value = value;
        }

        public Node(T value) {
            Value = value;
        }
    }

    private int _count;
    private volatile bool _isEmpty;
    private volatile Node? _head;

    public bool IsEmpty => _isEmpty;
    public int Count => _count;

    public ManyProducerManyConsumerStack() { }

    public ManyProducerManyConsumerStack(IEnumerable<T> items) {
        EnqueueRange(items);
    }

    public void Enqueue(T item) {
        var nextNode = new Node(ref item);
        Node? head;
        do {
            head = _head; //grab idempotency
            //transact
            nextNode.Next = head;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _head, value: nextNode, comparand: head), head));

        //transaction completed
        if (Interlocked.Increment(ref _count) == 1)
            _isEmpty = false;
    }

    public void Enqueue(ref T item) {
        var nextNode = new Node(ref item);
        Node? head;
        do {
            head = _head; //grab idempotency
            //transact
            nextNode.Next = head;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _head, value: nextNode, comparand: head), head));

        //transaction completed
        if (Interlocked.Increment(ref _count) == 1)
            _isEmpty = false;
    }

    public void EnqueueRange<TEnumerable>(TEnumerable items) where TEnumerable : IEnumerable<T> {
        #if NET6_0_OR_GREATER
        Unsafe.SkipInit(out Node nextNode);
        Unsafe.SkipInit(out Node lastNode);
        #else
        Node nextNode = null;
        Node lastNode = null;
        #endif
        int added = 0;
        foreach (var item in items) {
            if (++added == 1) {
                lastNode = nextNode = new Node(item);
            } else {
                var newNode = new Node(item);
                lastNode.Next = newNode;
                lastNode = newNode;
            }
        }

        if (added == 0)
            return;

        Node? head;
        do {
            head = _head; //grab idempotency
            //transact
            lastNode.Next = head;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _head, value: nextNode, comparand: head), head));

        //transaction completed
        if (Interlocked.Add(ref _count, added) == added)
            _isEmpty = false;
    }

    public bool TryDequeue(out T? item) {
        Node? curr, next;
        do {
            curr = _head; //take idempotency
            if (curr == null) {
                item = default;
                return false;
            }

            //transact
            next = curr.Next;
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref _head, value: next, comparand: curr), curr));

        //transaction completed
        if (Interlocked.Decrement(ref _count) == 0)
            _isEmpty = true;

        item = curr.Value;
        return true;
    }

    public void Dispose() {
        _head = null;
        _count = 0;
    }

    public void Clear() {
        Interlocked.Exchange(ref _head, null);
        var cnt = _count;
        Interlocked.Add(ref _count, -cnt);
    }
}