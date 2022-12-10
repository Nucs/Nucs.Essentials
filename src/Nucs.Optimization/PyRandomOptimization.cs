using Nucs.Optimization.Analayzer;
using Python.Runtime;

namespace Nucs.Optimization;

/// <summary>
///     A wrapper for bayesian optimization using python.
/// </summary>
/// <typeparam name="TParams">A class and new() for parameters</typeparam>
public class PyRandomOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    public PyRandomOptimization(ScoreFunction blackBoxScoreFunction, bool maximize = false) : base(blackBoxScoreFunction, maximize) { }

    public (double Score, TParams Parameters) Search(int n_calls, int? random_state = null, bool verbose = true) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");
        var result = skopt.dummy_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls,
                                          random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None, verbose: verbose);
        
        var scores = result.func_vals;
        var scoreParameters = result.x_iters;
        var best = np.argmin(scores);

        //unbox the best parameters
        List<Tuple<string, object>> values = (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, scoreParameters[best])
                                                                                  .AsManagedObject(typeof(List<Tuple<string, object>>));
        var bestParameters = ParametersAnalyzer<TParams>.Populate(values);
        
        //adjust score polarity to the goal
        double score = (double) scores[best] * (_maximize ? -1 : 1);

        //return the best score and the parameters
        return (Score: score, Parameters: bestParameters);
    }
}