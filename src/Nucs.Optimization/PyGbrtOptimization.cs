using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

public static class PyGbrtOptimization {
    public enum InitialPointGenerator {
        random,
        sobol,
        halton,
        hammersly,
        lhs,
        grid,
    }

    public enum AcqFunc {
        gp_hedge,
        EI,
        PI,
        LCB,
        EIps,
        PIps
    }

    public enum AcqOptimizer {
        sampling,
        lbfgs,
        auto
    }

    public enum BaseEstimator {
        RF,
        ET,
    }
}

/// <summary>
///     A wrapper for random/extra forest optimization using python.
/// </summary>
/// <typeparam name="TParams">A class and new() for parameters</typeparam>
public class PyGbrtOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    private readonly dynamic _forest;

    public PyGbrtOptimization(ScoreFunctionDelegate blackBoxScoreFunction, bool maximize = false, FileInfo? dumpResults = null) : base(blackBoxScoreFunction, maximize, dumpResults) {
        _forest = PyModule.FromString("forest", EmbeddedResourceHelper.ReadEmbeddedResource("forest.py")!);
    }

    public (double Score, TParams Parameters) Search(int n_calls, int n_random_starts, PyGbrtOptimization.BaseEstimator base_estimator = PyGbrtOptimization.BaseEstimator.ET,
                                                     PyGbrtOptimization.InitialPointGenerator initial_point_generator = PyGbrtOptimization.InitialPointGenerator.random,
                                                     PyGbrtOptimization.AcqFunc acq_func = PyGbrtOptimization.AcqFunc.LCB, PyGbrtOptimization.AcqOptimizer acq_optimizer = PyGbrtOptimization.AcqOptimizer.lbfgs,
                                                     int? random_state = null, int n_points = 10000, double xi = 0.01d, double kappa = 1.96d, bool verbose = false) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");

        dynamic? result = skopt.gbrt_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls, n_random_starts: n_random_starts,
                                              initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                              n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                              n_points: n_points, xi: xi, kappa: kappa, verbose: verbose);
        var scores = result.func_vals;
        var scoreParameters = result.x_iters;
        var best = np.argmin(scores);

        TryDumpResults(skopt, result);

        //unbox the best parameters
        List<Tuple<string, object>> values = (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, scoreParameters[best])
                                                                                  .AsManagedObject(typeof(List<Tuple<string, object>>));
        var bestParameters = ParametersAnalyzer<TParams>.Populate(values);

        //adjust score polarity to the goal
        double score = (double) scores[best] * (_maximize ? -1 : 1);

        //return the best score and the parameters
        return (Score: score, Parameters: bestParameters);
    }

    public (double Score, TParams Parameters)[] TopSearch(int topResults, int n_calls, int n_random_starts, PyGbrtOptimization.BaseEstimator base_estimator = PyGbrtOptimization.BaseEstimator.ET,
                                                          PyGbrtOptimization.InitialPointGenerator initial_point_generator = PyGbrtOptimization.InitialPointGenerator.random,
                                                          PyGbrtOptimization.AcqFunc acq_func = PyGbrtOptimization.AcqFunc.LCB, PyGbrtOptimization.AcqOptimizer acq_optimizer = PyGbrtOptimization.AcqOptimizer.lbfgs,
                                                          int? random_state = null, int n_points = 10000, double xi = 0.01d, double kappa = 1.96d, bool verbose = false) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");

        var result = skopt.gbrt_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls, n_random_starts: n_random_starts,
                                         initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                         n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                         n_points: n_points, xi: xi, kappa: kappa, verbose: verbose);
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

        Array.Sort(returns, (tuple, valueTuple) => valueTuple.Score.CompareTo(tuple.Score));

        //return the best score and the parameters
        return returns;
    }

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (disposing) {
            _forest.Dispose();
        }
    }
}