using Nucs.Optimization.Attributes;
using Python.Runtime;

namespace Nucs.Optimization.Analyzer;

public abstract class NumericalParameterType : ParameterType {
    protected NumericalParameterType(string name, TypeCode type, DimensionAttribute space) : base(name, type, space) { }
}

public class NumericalParameterType<T> : NumericalParameterType where T : INumber<T>, IMinMaxValue<T> {
    internal delegate void AssignDelegate<in TParams>(TParams parameters, T value);
    internal readonly Delegate AssignPointer;

    public void Assign<TParams>(TParams parameters, T value) {
        ((NumericalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, value);
    }

    public override void Assign<TParams>(TParams parameters, object value) {
        if (value is T tval) {
            ((NumericalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, tval);
        } else
            try {
                ((NumericalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, (T) Convert.ChangeType(value, typeof(T)));
            } catch (InvalidCastException e) {
                if (value is PyObject po) {
                    throw new InvalidCastException($"Unable to parse {po.ToString()} pyobject of type {po.GetPythonType().Name}", e);
                } else
                    throw;
            }
    }

    public NumericalParameterType(string name, TypeCode type, Delegate assignPointer, DimensionAttribute space) : base(name, type, space) {
        AssignPointer = assignPointer;
    }

    public override Type ValueType => typeof(T);
}