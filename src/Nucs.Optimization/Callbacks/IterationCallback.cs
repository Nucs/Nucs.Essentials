using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Gets called every iteration with the current best parameters and their score.
/// </summary>
public class IterationCallback<TParams> : PyOptCallback where TParams : class, new() {
    protected readonly bool _maximize;
    protected readonly PyModule _helperModule;
    public readonly StopConditionDelegate Callback;
    private int _iteration;
    public delegate void StopConditionDelegate(int iteration, TParams parameters, double score);

    public IterationCallback(PyModule helperModule, bool maximize, StopConditionDelegate callback) {
        _maximize = maximize;
        _helperModule = helperModule;
        Callback = callback;
        This = helperModule.Get("EarlyStopperWrapper").Invoke(Array.Empty<PyObject>(), Py.kw("callback", UnboxResults));
    }

    public IterationCallback(bool maximize, StopConditionDelegate callback) : this(PyModule.FromString("helper", EmbeddedResourceHelper.ReadEmbeddedResource("opt_helpers.py")!), maximize, callback) { }

    private bool? UnboxResults(dynamic result) {
        dynamic _helper = _helperModule;

        PyObject recentIter = result.x_iters[result.x_iters.__len__() - 1];
        //recentIter = recentIter[recentIter.InvokeMethod("__len__").ToInt32(CultureInfo.InvariantCulture) - 1];
        var parameters = ParametersAnalyzer<TParams>.Populate(values: (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, recentIter)
                                                                                                           .AsManagedObject(typeof(List<Tuple<string, object>>)));
        var score = (_maximize ? -1 : 1) * (double) result.func_vals[result.func_vals.__len__() - 1];
        //unbox results
        Callback(++_iteration, parameters, score);
        return null;
    }
}