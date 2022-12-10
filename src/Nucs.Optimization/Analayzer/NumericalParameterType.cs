using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Python.Runtime;
using RangeAttribute = Nucs.Optimization.Attributes.RangeAttribute;

namespace Nucs.Optimization.Analayzer;

public abstract class NumericalParameterType : ParameterType {
    public double UpperThreshold;
    public double LowerThreshold;

    protected NumericalParameterType(string name, TypeCode type) : base(name, type) { }
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

    public NumericalParameterType(string name, TypeCode type, Delegate assignPointer, T lowerThreshold, T upperThreshold) : base(name, type) {
        AssignPointer = assignPointer;
        LowerThreshold = double.CreateChecked(lowerThreshold);
        UpperThreshold = double.CreateChecked(upperThreshold);
    }

    public NumericalParameterType(string name, TypeCode type, Delegate assignPointer, RangeAttribute range)
        : this(name, type, assignPointer, range.GetMinimum<T>(), range.GetMaximum<T>()) { }

    public override Type ValueType => typeof(T);
}