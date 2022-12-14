using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Callbacks;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

public static class PyBayesianOptimization {
    public enum InitialPointGenerator {
        random,
        sobol,
        halton,
        hammersly,
        lhs,
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
}

/// <summary>
///     A wrapper for bayesian optimization using python.
/// </summary>
/// <typeparam name="TParams">A class and new() for parameters</typeparam>
public class PyBayesianOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    public PyBayesianOptimization(ScoreFunctionDelegate blackBoxScoreFunction, bool maximize = false, FileInfo? dumpResults = null) : base(blackBoxScoreFunction, maximize, dumpResults) { }

    public (double Score, TParams Parameters) Search(int n_calls, int n_random_starts, PyBayesianOptimization.InitialPointGenerator initial_point_generator = PyBayesianOptimization.InitialPointGenerator.random,
                                                     PyBayesianOptimization.AcqFunc acq_func = PyBayesianOptimization.AcqFunc.gp_hedge, PyBayesianOptimization.AcqOptimizer acq_optimizer = PyBayesianOptimization.AcqOptimizer.lbfgs,
                                                     int? random_state = null, int n_points = 10000, int n_restarts_optimizer = 5, double xi = 0.01d, double kappa = 1.96d, bool verbose = false, IEnumerable<PyOptCallback>? callbacks = null) {
        return SearchTop(1, n_calls, n_random_starts, initial_point_generator, acq_func, acq_optimizer, random_state, n_points, n_restarts_optimizer, xi, kappa, verbose, callbacks)[0];
    }

    public OptimizeResult<TParams> SearchAll(int n_calls, int n_random_starts, PyBayesianOptimization.InitialPointGenerator initial_point_generator = PyBayesianOptimization.InitialPointGenerator.random,
                                             PyBayesianOptimization.AcqFunc acq_func = PyBayesianOptimization.AcqFunc.gp_hedge, PyBayesianOptimization.AcqOptimizer acq_optimizer = PyBayesianOptimization.AcqOptimizer.lbfgs,
                                             int? random_state = null, int n_points = 10000, int n_restarts_optimizer = 5, double xi = 0.01d, double kappa = 1.96d, bool verbose = false, IEnumerable<PyOptCallback>? callbacks = null) {
        using dynamic skopt = PyModule.Import("skopt");
        var result = skopt.gp_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls, n_random_starts: n_random_starts,
                                       initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                       acq_optimizer: acq_optimizer.AsString(), n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                       n_points: n_points, n_restarts_optimizer: n_restarts_optimizer, xi: xi, kappa: kappa, verbose: verbose, callback: callbacks?.Select(p=>p.This).ToPyList());

        TryDumpResults(skopt, result);

        return new OptimizeResult<TParams>(result, _maximize);
    }

    public (double Score, TParams Parameters)[] SearchTop(int topResults, int n_calls, int n_random_starts, PyBayesianOptimization.InitialPointGenerator initial_point_generator = PyBayesianOptimization.InitialPointGenerator.random,
                                                          PyBayesianOptimization.AcqFunc acq_func = PyBayesianOptimization.AcqFunc.gp_hedge, PyBayesianOptimization.AcqOptimizer acq_optimizer = PyBayesianOptimization.AcqOptimizer.lbfgs,
                                                          int? random_state = null, int n_points = 10000, int n_restarts_optimizer = 5, double xi = 0.01d, double kappa = 1.96d, bool verbose = false, IEnumerable<PyOptCallback>? callbacks = null) {
        using dynamic skopt = PyModule.Import("skopt");
        using dynamic np = PyModule.Import("numpy");
        var result = skopt.gp_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls, n_random_starts: n_random_starts,
                                       initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                       acq_optimizer: acq_optimizer.AsString(), n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                       n_points: n_points, n_restarts_optimizer: n_restarts_optimizer, xi: xi, kappa: kappa, verbose: verbose, callback: callbacks?.Select(p=>p.This).ToPyList());
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