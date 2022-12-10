using Nucs.Optimization.Analayzer;
using Python.Runtime;

namespace Nucs.Optimization;

public static class PyBasyesianOptimization {
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
public class PyBasyesianOptimization<TParams> : PyOptimization<TParams> where TParams : class, new() {
    public PyBasyesianOptimization(ScoreFunction blackBoxScoreFunction, bool maximize = false) : base(blackBoxScoreFunction, maximize) { }

    public (double Score, TParams Parameters) Search(int n_calls, int n_random_starts, PyBasyesianOptimization.InitialPointGenerator initial_point_generator = PyBasyesianOptimization.InitialPointGenerator.random,
                                                     PyBasyesianOptimization.AcqFunc acq_func = PyBasyesianOptimization.AcqFunc.gp_hedge, PyBasyesianOptimization.AcqOptimizer acq_optimizer = PyBasyesianOptimization.AcqOptimizer.lbfgs,
                                                     int? random_state = 0, int n_points = 10000, int n_restarts_optimizer = 5, double xi = 0.01d, double kappa = 1.96d, bool verbose = true) {
        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        using dynamic np = Python.Runtime.PyModule.Import("numpy");
        var result = skopt.gp_minimize(wrappedScoreMethod, _searchSpace, n_calls: n_calls, n_random_starts: n_random_starts, random_state: 0,
                                       initial_point_generator: initial_point_generator.AsString(), acq_func: acq_func.AsString(),
                                       acq_optimizer: acq_optimizer.AsString(), n_jobs: 1, random_state: random_state != null ? new PyInt(random_state.Value) : PyObject.None,
                                       n_points: n_points, n_restarts_optimizer: n_restarts_optimizer, xi: xi, kappa: kappa, verbose: verbose);
        var scores = result.func_vals;
        var scoreParameters = result.x_iters;
        var best = np.argmin(scores);

        var parameters = new TParams();
        List<Tuple<string, object>> values = (List<Tuple<string, object>>) _helper.unbox_params(ParametersAnalyzer<TParams>.ParameterNames, scoreParameters[best])
                                                                                  .AsManagedObject(typeof(List<Tuple<string, object>>));

        //adjust score polarity to the goal
        double score = (double) scores[best] * (_maximize ? -1 : 1);
        return (Score: score, Parameters: ParametersAnalyzer<TParams>.Populate(values));
    }
}