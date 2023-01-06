using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
#if !NET6_0_OR_GREATER
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource<bool>;
#endif

namespace Nucs.Threading;


public delegate void ConcurrentThreadGunDelegate(int threadNumber);

/// <summary>
///     Orchestrates a highly syncronized parallel call which moves forward and enters code in a precise degree of parallelism.
///     Good at exposing parallelism bugs by forcing hotspot-activity-like.
/// </summary>
public class ConcurrentThreadGun : IDisposable {
    private readonly SemaphoreSlim _barrierThreadstarted;
    private readonly ManualResetEventSlim _barrierCorestart;
    private readonly SemaphoreSlim _barrierDone;
    #if !NET6_0_OR_GREATER
    private readonly TaskCompletionSource<bool> _completionSource;
    #else
    private readonly TaskCompletionSource _completionSource;
    #endif

    /// <summary>
    ///     How many threads this gun fired together.
    /// </summary>
    public int ThreadCount { get; }

    /// <summary>
    ///     The threads this gun allocated.
    /// </summary>
    public Thread[] Threads { get; }

    /// <summary>
    ///     The exceptions this gun has collected from the run.
    /// </summary>
    public Exception?[] Exceptions { get; }

    /// <summary>
    ///     Runs after all threads have finished.
    /// </summary>
    public Action<ConcurrentThreadGun>? PostRun { get; set; }

    /// <summary>
    ///     A task that completes when all threads have finished.
    /// </summary>
    public Task Completion => _completionSource.Task;

    #region Static

    [DebuggerHidden]
    public static void Run(int threadCount, ConcurrentThreadGunDelegate workload) {
        if (workload == null) throw new ArgumentNullException(nameof(workload));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        new ConcurrentThreadGun(threadCount).Run(workload);
    }

    [DebuggerHidden]
    public static void Run(int threadCount, params ConcurrentThreadGunDelegate[] workloads) {
        if (workloads == null) throw new ArgumentNullException(nameof(workloads));
        if (workloads.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(workloads));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        new ConcurrentThreadGun(threadCount).Run(workloads);
    }

    [DebuggerHidden]
    public static void Run(int threadCount, ConcurrentThreadGunDelegate workload, Action<ConcurrentThreadGun> postRun) {
        if (workload == null) throw new ArgumentNullException(nameof(workload));
        if (postRun == null) throw new ArgumentNullException(nameof(postRun));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        new ConcurrentThreadGun(threadCount) { PostRun = postRun }.Run(workload);
    }

    [DebuggerHidden]
    public static Task RunAsync(int threadCount, ConcurrentThreadGunDelegate workload) {
        if (workload == null) throw new ArgumentNullException(nameof(workload));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        return new ConcurrentThreadGun(threadCount).RunAsync(workload);
    }

    [DebuggerHidden]
    public static Task RunAsync(int threadCount, params ConcurrentThreadGunDelegate[] workloads) {
        if (workloads == null) throw new ArgumentNullException(nameof(workloads));
        if (workloads.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(workloads));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        return new ConcurrentThreadGun(threadCount).RunAsync(workloads);
    }

    [DebuggerHidden]
    public static Task RunDiverseAsync(int threadCount, params ConcurrentThreadGunDelegate[] workloads) {
        if (workloads == null) throw new ArgumentNullException(nameof(workloads));
        if (workloads.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(workloads));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));

        if (threadCount != workloads.Length) {
            if (threadCount % (decimal) workloads.Length != 0) {
                throw new ArgumentException($"Diverse thread count ({threadCount}) must be divisible by workloads count ({workloads.Length}).");
            }

            workloads = Enumerable.Repeat(workloads, threadCount / workloads.Length).SelectMany(x => x).ToArray();
        }

        return new ConcurrentThreadGun(threadCount).RunAsync(workloads);
    }

    [DebuggerHidden]
    public static Task RunAsync(int threadCount, ConcurrentThreadGunDelegate workload, Action<ConcurrentThreadGun> postRun) {
        if (workload == null) throw new ArgumentNullException(nameof(workload));
        if (postRun == null) throw new ArgumentNullException(nameof(postRun));
        if (threadCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadCount));
        return new ConcurrentThreadGun(threadCount) { PostRun = postRun }.RunAsync(workload);
    }

    #endregion


    /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
    public ConcurrentThreadGun(int threadCount) {
        if (threadCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(threadCount));
        ThreadCount = threadCount;
        Threads = new Thread[ThreadCount];
        Exceptions = new Exception[ThreadCount];
        _completionSource = new TaskCompletionSource();
        _barrierDone = new SemaphoreSlim(0, threadCount);
        _barrierCorestart = new ManualResetEventSlim();
        _barrierThreadstarted = new SemaphoreSlim(0, threadCount);
    }

    [DebuggerHidden]
    public void Run(params ConcurrentThreadGunDelegate[] workloads) {
        if (workloads == null)
            throw new ArgumentNullException(nameof(workloads));
        if (workloads.Length != 1 && workloads.Length % ThreadCount != 0)
            throw new InvalidOperationException($"Run method must accept either 1 workload or n-threads workloads. Got {workloads.Length} workloads.");

        if (ThreadCount == 1) {
            Exception? ex = null;
            new Thread(() => {
                try {
                    workloads[0](0);
                } catch (Exception e) {
                    if (Debugger.IsAttached)
                        throw;
                    ex = e;
                } finally {
                    _barrierDone.Release(1);
                }
            }).Start();

            if (ex != null) {
                ex = new Exception($"Thread 0 has failed: ", ex);
                _completionSource.TrySetException(ex);
                throw ex;
            }

            PostRun?.Invoke(this);
            
            #if !NET6_0_OR_GREATER
            _completionSource.TrySetResult(true);
            #else
            _completionSource.TrySetResult();
            #endif
            return;
        }

        //thread core
        Exception? ThreadCore(ConcurrentThreadGunDelegate core, int threadNumber) {
            _barrierThreadstarted.Release(1);
            _barrierCorestart.Wait();
            //workload
            try {
                core(threadNumber);
            } catch (Exception e) {
                if (Debugger.IsAttached)
                    throw;
                return e;
            } finally {
                _barrierDone.Release(1);
            }

            return null;
        }

        //initialize all threads
        if (workloads.Length == 1) {
            var workload = workloads[0];
            for (int i = 0; i < ThreadCount; i++) {
                var i_local = i;
                Threads[i] = new Thread(() => Exceptions[i_local] = ThreadCore(workload, i_local));
            }
        } else {
            for (int i = 0; i < ThreadCount; i++) {
                var i_local = i;
                var workload = workloads[i_local % workloads.Length];
                Threads[i] = new Thread(() => Exceptions[i_local] = ThreadCore(workload, i_local));
            }
        }

        //run all threads
        for (int i = 0; i < ThreadCount; i++) Threads[i].Start();
        //wait for threads to be started and ready
        for (int i = 0; i < ThreadCount; i++) _barrierThreadstarted.Wait();

        //signal threads to start
        _barrierCorestart.Set();

        //wait for threads to finish
        for (int i = 0; i < ThreadCount; i++) _barrierDone.Wait();

        //handle fails
        int fails = Exceptions.Count(e => e != null);
        if (fails != 0) {
            var ex = new AggregateException($"ConcurrentThreadGun has failed {fails} threads. See Inner Exceptions", Exceptions.Where(e => e != null).ToArray()!);
            _completionSource.TrySetException(ex);
            throw ex;
        }

        //checks after ended
        PostRun?.Invoke(this);
        #if !NET6_0_OR_GREATER
        _completionSource.TrySetResult(true);
        #else
        _completionSource.TrySetResult();
        #endif
    }

    public async Task RunAsync(params ConcurrentThreadGunDelegate[] workloads) {
        if (workloads == null)
            throw new ArgumentNullException(nameof(workloads));
        if (workloads.Length != 1 && workloads.Length % ThreadCount != 0)
            throw new InvalidOperationException($"Run method must accept either 1 workload or n-threads workloads. Got {workloads.Length} workloads.");

        if (ThreadCount == 1) {
            Exception? ex = null;
            new Thread(() => {
                try {
                    workloads[0](0);
                } catch (Exception e) {
                    if (Debugger.IsAttached)
                        throw;
                    ex = e;
                } finally {
                    _barrierDone.Release(1);
                }
            }).Start();

            await _barrierDone.WaitAsync().ConfigureAwait(false);

            if (ex != null) {
                ex = new Exception($"Thread 0 has failed: ", ex);
                _completionSource.TrySetException(ex);
                throw ex;
            }

            PostRun?.Invoke(this);
            #if !NET6_0_OR_GREATER
            _completionSource.TrySetResult(true);
            #else
            _completionSource.TrySetResult();
            #endif
            return;
        }

        //thread core
        Exception? ThreadCore(ConcurrentThreadGunDelegate core, int threadNumber) {
            _barrierThreadstarted.Release(1);
            _barrierCorestart.Wait();
            //workload
            try {
                core(threadNumber);
            } catch (Exception e) {
                if (Debugger.IsAttached)
                    throw;
                return e;
            } finally {
                _barrierDone.Release(1);
            }

            return null;
        }

        //initialize all threads
        if (workloads.Length == 1) {
            var workload = workloads[0];
            for (int i = 0; i < ThreadCount; i++) {
                var i_local = i;
                Threads[i] = new Thread(() => Exceptions[i_local] = ThreadCore(workload, i_local));
            }
        } else {
            for (int i = 0; i < ThreadCount; i++) {
                var i_local = i;
                var workload = workloads[i_local % workloads.Length];
                Threads[i] = new Thread(() => Exceptions[i_local] = ThreadCore(workload, i_local));
            }
        }

        //run all threads
        for (int i = 0; i < ThreadCount; i++) Threads[i].Start();
        //wait for threads to be started and ready
        for (int i = 0; i < ThreadCount; i++)
            await _barrierThreadstarted.WaitAsync().ConfigureAwait(false);

        //signal threads to start
        _barrierCorestart.Set();

        //wait for threads to finish
        for (int i = 0; i < ThreadCount; i++)
            await _barrierDone.WaitAsync().ConfigureAwait(false);

        //handle fails
        int fails = Exceptions.Count(e => e != null);
        if (fails != 0) {
            var ex = new AggregateException($"ConcurrentThreadGun has failed {fails} threads. See Inner Exceptions", Exceptions.Where(e => e != null).ToArray()!);
            _completionSource.TrySetException(ex);
            throw ex;
        }

        //checks after ended
        PostRun?.Invoke(this);
        #if !NET6_0_OR_GREATER
        _completionSource.TrySetResult(true);
        #else
        _completionSource.TrySetResult();
        #endif
    }

    public void Dispose() {
        _barrierThreadstarted.Dispose();
        _barrierCorestart.Dispose();
        _barrierDone.Dispose();
        _completionSource.TrySetCanceled();
    }
}