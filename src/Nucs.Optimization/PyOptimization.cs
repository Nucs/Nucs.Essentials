using Nucs.Optimization.Analyzer;
using Nucs.Optimization.Attributes;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

/// <summary>
///     Abstract class for all python based optimization algorithms.
/// </summary>
/// <typeparam name="TParams"></typeparam>
public abstract class PyOptimization<TParams> : IDisposable where TParams : class, new() {
    protected readonly FileInfo? DumpResults;
    protected readonly ScoreFunctionDelegate _blackBoxScoreFunction;
    protected readonly dynamic _helper;
    protected readonly PyList _searchSpace;
    protected readonly PyObject wrappedScoreMethod;
    protected readonly bool _maximize;

    /// <summary>
    ///     The score function that will be used to evaluate the parameters.
    /// </summary>
    public delegate double ScoreFunctionDelegate(TParams parameters);

    protected PyOptimization(ScoreFunctionDelegate blackBoxScoreFunction, bool maximize, FileInfo? dumpResults = null) {
        //ensure analyzer constructed
        ParametersAnalyzer<TParams>.Initialize();

        //process blackbox function and analyze attributes
        _blackBoxScoreFunction = blackBoxScoreFunction;
        DumpResults = dumpResults;
        if (blackBoxScoreFunction.Method.GetCustomAttribute<MinimizeAttribute>() != null)
            maximize = false;
        else if (blackBoxScoreFunction.Method.GetCustomAttribute<MaximizeAttribute>() != null)
            maximize = true;

        _maximize = maximize;

        //load helper script
        _helper = PyModule.FromString("helper", EmbeddedResourceHelper.ReadEmbeddedResource("opt_helpers.py")!);

        //wrap blackbox function
        wrappedScoreMethod = _helper.scoreWrapper(PyObject.FromManagedObject(UnboxParametersScoreMethod), ParametersAnalyzer<TParams>.ParameterNames, _maximize);

        //create search space
        _searchSpace = new PyList();
        CreateSearchSpaceParameters();
    }

    /// <summary>
    ///     Creates the search space parameters from <see cref="ParametersAnalyzer{TParams}" />.
    /// </summary>
    protected virtual void CreateSearchSpaceParameters() {
        using dynamic skopt_space = PyModule.Import("skopt.space");
        foreach (var parameter in ParametersAnalyzer<TParams>.Parameters) {
            switch (parameter.Value) {
                case CategoricalParameterType type: {
                    var space = (CategoricalSpace) type.Space;
                    //space.Categorical([1, 2, 3], name='');
                    _searchSpace.Append(skopt_space.Categorical(categories: new PyList(space.ObjectCategories.Select(o => o.ToPython()).ToArray()),
                                                                name: parameter.Key,
                                                                transform: space.Transform.AsString().ToLowerInvariant(),
                                                                prior: space.Prior));
                    break;
                }
                case NumericalParameterType type: {
                    var space = (NumericalSpace) type.Space;

                    if (type.IsFloating) {
                        //space.Real (min, max, name='');
                        _searchSpace.Append(skopt_space.Real(low: space.GetLow(), high: space.GetHigh(),
                                                             name: parameter.Key,
                                                             prior: space.Prior.AsString().ToLowerInvariant(),
                                                             @base: space.Base,
                                                             transform: space.Transform.AsString().ToLowerInvariant()));
                    } else {
                        //space.Integer(min, max, name='');
                        _searchSpace.Append(skopt_space.Integer(low: space.GetLow(), high: space.GetHigh(),
                                                                name: parameter.Key,
                                                                prior: space.Prior.AsString().ToLowerInvariant(),
                                                                @base: space.Base,
                                                                transform: space.Transform.AsString().ToLowerInvariant()));
                    }

                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected virtual void TryDumpResults(dynamic skopt, dynamic result) {
        if (DumpResults != null) {
            Directory.CreateDirectory(DumpResults.Directory!.FullName);
            skopt.utils.dump(result, DumpResults.FullName, store_objective: false);
        }
    }

    public static OptimizeResult<TParams> Load(FileInfo file, bool maximize) {
        ParametersAnalyzer<TParams>.Initialize();

        var path = file.FullName;
        if (!Path.Exists(path))
            throw new FileNotFoundException($"File {path} not found.");

        using dynamic skopt = Python.Runtime.PyModule.Import("skopt");
        return new OptimizeResult<TParams>(skopt.utils.load(path), maximize);
    }

    /// <summary>
    ///     Wraps the blackbox function to be used by the python optimizer.
    /// </summary>
    protected virtual double UnboxParametersScoreMethod(PyObject args) {
        return _blackBoxScoreFunction(ParametersAnalyzer<TParams>.Populate((List<Tuple<string, object>>) args.AsManagedObject(typeof(List<Tuple<string, object>>))!));
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            _searchSpace.Dispose();
            wrappedScoreMethod.Dispose();
            _helper.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}