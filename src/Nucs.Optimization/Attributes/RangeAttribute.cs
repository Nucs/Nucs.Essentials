using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Nucs.Optimization.Attributes;

/// <summary>
///     Used for specifying a range constraint
/// </summary>
public abstract class RangeAttribute : ValidationAttribute {
    /// <summary>
    ///     Gets the minimum value for the range
    /// </summary>
    public abstract T GetMinimum<T>() where T : INumber<T>, IMinMaxValue<T>;

    /// <summary>
    ///     Gets the maximum value for the range
    /// </summary>
    public abstract T GetMaximum<T>() where T : INumber<T>, IMinMaxValue<T>;

    /// <summary>
    ///     Gets the type of the <see cref="Minimum" /> and <see cref="Maximum" /> values (e.g. Int32, Double, or some custom
    ///     type)
    /// </summary>
    public abstract Type OperandType { get; }
}

/// <summary>
///     Used for specifying a range constraint
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class RangeAttribute<T> : RangeAttribute where T : INumber<T>, IMinMaxValue<T> {
    /// <summary>
    ///     Constructor that takes integer minimum and maximum values
    /// </summary>
    /// <param name="minimum">The minimum value, inclusive</param>
    /// <param name="maximum">The maximum value, inclusive</param>
    public RangeAttribute(T minimum, T maximum)
        : base() {
        Minimum = minimum;
        Maximum = maximum;
        if (minimum.CompareTo(maximum) > 0) {
            throw new InvalidOperationException("Minimum value must be less than or equal to maximum value.");
        }
    }

    /// <summary>
    ///     Gets the minimum value for the range
    /// </summary>
    public T Minimum { get; private set; }

    /// <summary>
    ///     Gets the maximum value for the range
    /// </summary>
    public T Maximum { get; private set; }

    /// <summary>
    ///     Returns true if the value falls between min and max, inclusive.
    /// </summary>
    /// <param name="value">The value to test for validity.</param>
    /// <returns><c>true</c> means the <paramref name="value" /> is valid</returns>
    /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
    public override bool IsValid(object? value) {
        // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
        if (value == null || (value as string)?.Length == 0) {
            return true;
        }

        IComparable<T>? convertedValue;

        try {
            convertedValue = (IComparable<T>) Convert.ChangeType(value, typeof(T));
        } catch (Exception) {
            return false;
        }

        var min = (IComparable) Minimum;
        var max = (IComparable) Maximum;
        return min.CompareTo(convertedValue) <= 0 && max.CompareTo(convertedValue) >= 0;
    }

    public override T1 GetMinimum<T1>() {
        return T1.CreateChecked(Minimum);
    }

    public override T1 GetMaximum<T1>() {
        return T1.CreateChecked(Maximum);
    }

    public override Type OperandType => typeof(T);
}