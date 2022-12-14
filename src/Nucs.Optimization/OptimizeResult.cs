using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

/// <summary>
///     The result of an optimization run.
/// </summary>
/// <typeparam name="TParams">The parameters this optimization used</typeparam>
public class OptimizeResult<TParams> where TParams : class, new() {
    /// <summary>
    ///     The best parameters found.
    /// </summary>
    public TParams Best { get; }

    /// <summary>
    ///     The best score found.
    /// </summary>
    public double BestScore { get; }

    /// <summary>
    ///     All parameters and their scores from the optimization run.
    ///     Ordered by score. Index 0 is better.
    ///     Best/BestScore are included and are the same as this[0].
    /// </summary>
    public (TParams Parameters, double Score)[] Iterations { get; }

    public OptimizeResult(dynamic result, bool maximize, bool? descending = null) {
        using dynamic helper = PyModule.FromString("helper", EmbeddedResourceHelper.ReadEmbeddedResource("opt_helpers.py")!);
        BestScore = ((double) result.fun) * (maximize ? -1 : 1);
        Best = ParametersAnalyzer<TParams>.Populate((List<Tuple<string, object>>) helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, result.x)
                                                                                        .AsManagedObject(typeof(List<Tuple<string, object>>)));
        var results = (int) result.func_vals.__len__();
        Iterations = new (TParams Parameters, double Score)[results];
        for (var i = 0; i < results; i++) {
            var score = ((double) result.func_vals[i]) * (maximize ? -1 : 1);
            var parameters = ParametersAnalyzer<TParams>.Populate((List<Tuple<string, object>>) helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, result.x_iters[i])
                                                                                                      .AsManagedObject(typeof(List<Tuple<string, object>>)));
            Iterations[i] = (parameters, score);
        }

        Array.Sort(Iterations, (descending ?? maximize) ? (lhs, rhs) => rhs.Score.CompareTo(lhs.Score) : (lhs, rhs) => lhs.Score.CompareTo(rhs.Score));
    }

    public OptimizeResult(TParams best, double bestScore, (TParams Parameters, double Score)[] iterations) {
        Best = best;
        BestScore = bestScore;
        Iterations = iterations;
    }
}