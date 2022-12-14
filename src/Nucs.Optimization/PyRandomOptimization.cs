using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Callbacks;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

/// <summary>
///     A wrapper for random search optimization using python.
/// </summary>
/// <typeparam name="TParams">A class and new() for parameters</typeparam>
public class PyRandomOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    public PyRandomOptimization(ScoreFunctionDelegate blackBoxScoreFunction, bool maximize = false, FileInfo? dumpResults = null) : base(blackBoxScoreFunction, maximize, dumpResults) { }

    public (double Score, TParams Parameters) Search(int n_calls, int? random_state = null, bool verbose = true, IEnumerable<PyOptCallback>? callbacks = null) {
        return SearchTop(1, n_calls, random_state, verbose, callbacks)[0];
    }

    public OptimizeResult<TParams> SearchAll(int n_calls, int? random_state = null, bool verbose = true, IEnumerable<PyOptCallback>? callbacks = null) {
        using dynamic skopt = PyModule.Import("skopt");
        using dynamic np = PyModule.Import("numpy");
        var result = skopt.dummy_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls,
                                          random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None, verbose: verbose, callback: callbacks?.Select(p=>p.This).ToPyList());

        TryDumpResults(skopt, result);

        return new OptimizeResult<TParams>(result, _maximize);
    }

    public (double Score, TParams Parameters)[] SearchTop(int topResults, int n_calls, int? random_state = null, bool verbose = true, IEnumerable<PyOptCallback>? callbacks = null) {
        using dynamic skopt = PyModule.Import("skopt");
        using dynamic np = PyModule.Import("numpy");
        var result = skopt.dummy_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls,
                                          random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None, verbose: verbose, callback: callbacks?.Select(p=>p.This).ToPyList());

        var scores = result.func_vals;
        var scoreParameters = result.x_iters;

        TryDumpResults(skopt, result);

        int scoresCount = (int) scores.shape[0];
        dynamic best; //numpy array of indices
        if (topResults < scoresCount) {
            best = np.argpartition(scores, topResults);
        } else {
            best = np.arange(scoresCount);
        }

        topResults = Math.Min(topResults, scoresCount);

        var returns = new (double Score, TParams Parameters)[topResults];
        for (int i = 0; i < returns.Length; i++) {
            //unbox the best parameters
            List<Tuple<string, object>> values = (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, scoreParameters[best[i]])
                                                                                      .AsManagedObject(typeof(List<Tuple<string, object>>));
            var bestParameters = ParametersAnalyzer<TParams>.Populate(values);

            //adjust score polarity to the goal
            double score = (double) scores[best[i]] * (_maximize ? -1 : 1);
            returns[i] = (Score: score, Parameters: bestParameters);
        }
        
        Array.Sort(returns, _maximize ? (lhs, rhs) => rhs.Score.CompareTo(lhs.Score) : (lhs, rhs) => lhs.Score.CompareTo(rhs.Score));

        //return the best score and the parameters
        return returns;
    }
}