using System.ComponentModel.DataAnnotations;

namespace Nucs.Optimization.Attributes;

/// <summary>
/// Search space dimension that can take on integer values.
/// </summary>
/// <remarks>https://scikit-optimize.github.io/stable/_modules/skopt/space/space.html</remarks>
public abstract class IntegerSpace : NumericalSpace { }

/// <summary>
/// Search space dimension that can take on integer values.
/// </summary>
/// <remarks>https://scikit-optimize.github.io/stable/_modules/skopt/space/space.html</remarks>
public sealed class IntegerSpace<T> : IntegerSpace where T : IBinaryInteger<T> {
    public delegate T TransformDelegate(IntegerSpace<T> space, T value);

    /// <summary>
    /// Lower bound (inclusive).
    /// </summary>
    public T Low { get; init; }

    /// <summary>
    /// Upper bound (inclusive).
    /// </summary>
    public T High { get; init; }


    /// <summary>
    /// Initializes a new instance of the <see cref="IntegerSpace"/> class.
    /// </summary>
    /// <param name="low">Lower bound (inclusive).</param>
    /// <param name="high">Upper bound (inclusive).</param>
    public IntegerSpace(T low, T high) {
        this.Low = low;
        this.High = high;
    }

    public override Type DType => typeof(T);

    public override T1 GetLow<T1>() {
        return T1.CreateChecked(Low);
    }

    public override T1 GetHigh<T1>() {
        return T1.CreateChecked(High);
    }

    public override object GetLow() {
        return Low;
    }

    public override object GetHigh() {
        return High;
    }
}