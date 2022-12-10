using Nucs.Optimization.Analayzer;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

public static class PyForestOptimization {
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
public class PyForestOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    private readonly dynamic _forest;

    public PyForestOptimization(ScoreFunction blackBoxScoreFunction, bool maximize = false) : base(blackBoxScoreFunction, maximize) {
        _forest = PyModule.FromString("forest", EmbeddedResourceHelper.ReadEmbeddedResource("forest.py")!);
    }

    public (double Score, TParams Parameters) Search(int n_calls, int n_random_starts, PyForestOptimization.BaseEstimator base_estimator = PyForestOptimization.BaseEstimator.ET,
                                                     PyForestOptimization.InitialPointGenerator initial_point_generator = PyForestOptimization.InitialPointGenerator.random,
                                                     PyForestOptimization.AcqFunc acq_func = PyForestOptimization.AcqFunc.LCB, PyForestOptimization.AcqOptimizer acq_optimizer = PyForestOptimization.AcqOptimizer.lbfgs,
                                                     int? random_state = null, int n_points = 10000, double xi = 0.01d, double kappa = 1.96d, bool verbose = false) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");
        var estimator = base_estimator switch {
            PyForestOptimization.BaseEstimator.RF => _forest.RandomForestRegressor(criterion: "squared_error", random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None),
            PyForestOptimization.BaseEstimator.ET => _forest.ExtraTreesRegressor(criterion: "squared_error", random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None),
            _                                     => throw new ArgumentOutOfRangeException(nameof(base_estimator), base_estimator, null)
        };

        var result = skopt.forest_minimize(wrappedScoreMethod, _searchSpace, base_estimator: estimator, n_calls: n_calls, n_random_starts: n_random_starts,
                                           initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                           n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                           n_points: n_points, xi: xi, kappa: kappa, verbose: verbose);
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

    public (double Score, TParams Parameters)[] TopSearch(int topResults, int n_calls, int n_random_starts, PyForestOptimization.BaseEstimator base_estimator = PyForestOptimization.BaseEstimator.ET,
                                                          PyForestOptimization.InitialPointGenerator initial_point_generator = PyForestOptimization.InitialPointGenerator.random,
                                                          PyForestOptimization.AcqFunc acq_func = PyForestOptimization.AcqFunc.LCB, PyForestOptimization.AcqOptimizer acq_optimizer = PyForestOptimization.AcqOptimizer.lbfgs,
                                                          int? random_state = null, int n_points = 10000, double xi = 0.01d, double kappa = 1.96d, bool verbose = false) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");
        var estimator = base_estimator switch {
            PyForestOptimization.BaseEstimator.RF => _forest.RandomForestRegressor(criterion: "squared_error", random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None),
            PyForestOptimization.BaseEstimator.ET => _forest.ExtraTreesRegressor(criterion: "squared_error", random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None),
            _                                     => throw new ArgumentOutOfRangeException(nameof(base_estimator), base_estimator, null)
        };

        var result = skopt.forest_minimize(wrappedScoreMethod, _searchSpace, base_estimator: estimator, n_calls: n_calls, n_random_starts: n_random_starts,
                                           initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                           n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                           n_points: n_points, xi: xi, kappa: kappa, verbose: verbose);
        var scores = result.func_vals;
        var scoreParameters = result.x_iters;

        var best = np.argpartition(scores, topResults);

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