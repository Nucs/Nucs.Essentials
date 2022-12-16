using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Gets called every iteration with the current best parameters and their score.
/// </summary>
public class FastIterationCallback<TParams> : PyOptCallback where TParams : class, new() {
    public readonly StopConditionDelegate Callback;
    private int _iteration;

    public delegate void StopConditionDelegate(int iterations);

    public FastIterationCallback(PyModule helperModule, StopConditionDelegate callback) {
        Callback = callback;
        This = helperModule.Get("EarlyStopperWrapper").Invoke(Array.Empty<PyObject>(), Py.kw("callback", UnboxResults));
    }

    public FastIterationCallback(StopConditionDelegate callback) : this(PyModule.FromString("helper", EmbeddedResourceHelper.ReadEmbeddedResource("opt_helpers.py")!), callback) { }

    private bool? UnboxResults(PyObject result) {
        Callback(++_iteration);
        return null;
    }
}