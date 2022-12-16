using Nucs.Optimization.Attributes;

namespace Nucs.Optimization.Analayzer;

public abstract class CategoricalParameterType : ParameterType {
    public abstract object[] ObjectValues { get; }

    protected CategoricalParameterType(string name, TypeCode type, DimensionAttribute space) : base(name, type, space) { }
}

public class CategoricalParameterType<T> : CategoricalParameterType {
    public static bool IsEnum = typeof(T).IsEnum;
    internal delegate void AssignDelegate<in TParams>(TParams parameters, T value);
    public override object[] ObjectValues => ((CategoricalSpace<T>) Space).ObjectCategories;

    internal readonly Delegate AssignPointer;

    public void Assign<TParams>(TParams parameters, T value) {
        ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, value);
    }

    public override void Assign<TParams>(TParams parameters, object value) {
        if (IsEnum) {
            ((CategoricalParameterType<string>.AssignDelegate<TParams>) AssignPointer)(parameters, (string) value);
        } else if (value is T tval)
            ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, tval);
        else
            ((CategoricalParameterType<T>.AssignDelegate<TParams>) AssignPointer)(parameters, (T) Convert.ChangeType(value, typeof(T)));
    }

    public CategoricalParameterType(string name, TypeCode type, Delegate assignPointer, DimensionAttribute space) : base(name, type, space) {
        AssignPointer = assignPointer;
    }

    public override Type ValueType => typeof(T);
}