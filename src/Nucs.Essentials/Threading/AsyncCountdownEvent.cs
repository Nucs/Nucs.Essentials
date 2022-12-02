using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;

namespace Nucs.Threading;

/// <summary>
///     A countdown event counting down to zero. Good use case is for when you have N items to process and you want to wait for all of them to finish.
///     The number of items can be changed at any time, and the event will wait for the new number of items to finish aswell.
/// </summary>
public class AsyncCountdownEvent : IDisposable {
    private readonly AsyncManualResetEvent _event = new AsyncManualResetEvent(false);
    private volatile uint _remainingSignals;

    public uint RequiredSignals => _remainingSignals;

    public bool IsSet => _event.IsSet;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initialRemainingSignals"></param>
    public AsyncCountdownEvent(uint initialRemainingSignals = 0) {
        _remainingSignals = initialRemainingSignals;
        if (initialRemainingSignals == 0)
            _event.Set();
    }

    /// <summary>
    ///     Adds countdown that will require a <see cref="Signal"/> to be called.
    /// </summary>
    public void Add() {
        //increment countdown counter in threadsafe manner
        if (Interlocked.Add(ref _remainingSignals, 1) == 1)
            //if it was 0, we have to reset the countdown event to signal that there are requests pending/active
            _event.Reset();
    }

    /// <summary>
    ///     Adds countdown N <paramref name="times"/> that will require a <see cref="Signal"/> to be called.
    /// </summary>
    public void Add(uint times) {
        //increment countdown counter in threadsafe manner
        if (Interlocked.Add(ref _remainingSignals, times) == times)
            //if it was 0, we have to reset the countdown event to signal that there are requests pending/active
            _event.Reset();
    }

    /// <summary>
    ///     Signals to decrement <paramref name="times"/> countdown.
    /// </summary>
    public void Signal(uint times = 1) {
        //decrement countdown counter in threadsafe manner
        uint currentValue;
        uint originalValue;
        uint target;
        do {
            currentValue = _remainingSignals;
            target = Math.Max(currentValue - times, 0);
            originalValue = Interlocked.CompareExchange(ref _remainingSignals, value: target, comparand: currentValue);
        } while (originalValue != currentValue);

        if (target == 0)
            _event.Set();
    }

    public ValueTask<bool> WaitAsync(TimeSpan timeout, CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(timeout, token);
    }

    public ValueTask WaitAsync(CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(token);
    }

    public ValueTask<bool> WaitAsync<T>(Predicate<T> condition, T arg, TimeSpan timeout, CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(condition, arg, timeout, token);
    }

    public ValueTask WaitAsync<T>(Predicate<T> condition, T arg, CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(condition, arg, token);
    }

    public ValueTask<bool> WaitAsync<T1, T2>(Func<T1, T2, bool> condition, T1 arg1, T2 arg2, TimeSpan timeout, CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(condition, arg1, arg2, timeout, token);
    }

    public ValueTask WaitAsync<T1, T2>(Func<T1, T2, bool> condition, T1 arg1, T2 arg2, CancellationToken token = new CancellationToken()) {
        return _event.WaitAsync(condition, arg1, arg2, token);
    }


    public void Dispose() {
        _event.Dispose();
    }
}