using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Stop the optimization when the <see cref="StopConditionDelegate"/> provided returns true.
/// </summary>
public class EarlyStopper<TParams> : PyOptCallback where TParams : class, new() {
    protected readonly PyModule _helperModule;
    public readonly StopConditionDelegate Callback;
    protected readonly bool _maximize;

    public delegate bool StopConditionDelegate(TParams parameters, double score);

    public EarlyStopper(PyModule helperModule, bool maximize, StopConditionDelegate callback) {
        _maximize = maximize;
        _helperModule = helperModule;
        Callback = callback;
        This = helperModule.Get("EarlyStopperWrapper").Invoke(Array.Empty<PyObject>(), Py.kw("callback", UnboxResults));
    }

    public EarlyStopper(StopConditionDelegate callback, bool maximize) : this(PyModule.FromString("helper", EmbeddedResourceHelper.ReadEmbeddedResource("opt_helpers.py")!), maximize, callback) { }

    private bool? UnboxResults(dynamic result) {
        dynamic _helper = _helperModule;

        PyObject recentIter = result.x_iters[result.x_iters.__len__() - 1];
        //recentIter = recentIter[recentIter.InvokeMethod("__len__").ToInt32(CultureInfo.InvariantCulture) - 1];
        var parameters = ParametersAnalyzer<TParams>.Populate(values: (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, recentIter)
                                                                                                           .AsManagedObject(typeof(List<Tuple<string, object>>)));
        var score = (_maximize ? -1 : 1) * (double) result.func_vals[result.func_vals.__len__() - 1];
        //unbox results
        return Callback(parameters, score);
    }
}