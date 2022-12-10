using Nucs.Optimization.Analayzer;
using Nucs.Optimization.Attributes;
using Nucs.Optimization.Helper;
using Python.Runtime;

namespace Nucs.Optimization;

/// <summary>
///     Abstract class for all python based optimization algorithms.
/// </summary>
/// <typeparam name="TParams"></typeparam>
public abstract class PyOptimization<TParams> : IDisposable where TParams : class, new() {
    protected readonly ScoreFunction _blackBoxScoreFunction;
    protected readonly dynamic _helper;
    protected readonly PyList _searchSpace;
    protected readonly PyObject wrappedScoreMethod;
    protected readonly bool _maximize;

    /// <summary>
    ///     The score function that will be used to evaluate the parameters.
    /// </summary>
    public delegate double ScoreFunction(TParams parameters);

    protected PyOptimization(ScoreFunction blackBoxScoreFunction, bool maximize) {
        //ensure analyzer constructed
        ParametersAnalyzer<TParams>.Initialize();

        //process blackbox function and analyze attributes
        _blackBoxScoreFunction = blackBoxScoreFunction;
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
        using dynamic space = PyModule.Import("skopt.space");
        foreach (var parameter in ParametersAnalyzer<TParams>.Parameters) {
            switch (parameter.Value) {
                case CategoricalParameterType type:
                    //space.Categorical([1, 2, 3], name='');
                    _searchSpace.Append(space.Categorical(new PyList(type.ObjectValues.Select(o => o.ToPython()).ToArray()), name: parameter.Key));
                    break;
                case NumericalParameterType type:
                    if (type.IsFloating)
                        //space.Real(min, max, name='');
                        _searchSpace.Append(space.Real(low: type.LowerThreshold, high: type.UpperThreshold, name: parameter.Key));
                    else
                        //space.Integer(min, max, name='');
                        _searchSpace.Append(space.Integer(low: (int) type.LowerThreshold, high: (int) type.UpperThreshold, name: parameter.Key));
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
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