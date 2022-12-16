using System.ComponentModel.DataAnnotations;

namespace Nucs.Optimization.Attributes;

/// <summary>
/// Search space dimension that can take on any real value.
/// </summary>
/// <remarks>https://scikit-optimize.github.io/stable/_modules/skopt/space/space.html</remarks>
public abstract class NumericalSpace : DimensionAttribute {
    /// <summary>
    /// Distribution to use when sampling random integers for this dimension.
    /// If "uniform", integers are sampled uniformly between the lower and upper bounds.
    /// If "log-uniform", integers are sampled uniformly between log(lower, base) and log(upper, base) where log has base base.
    /// </summary>
    public Prior Prior { get; init; } = Prior.Uniform;

    /// <summary>
    /// The logarithmic base to use for a log-uniform prior.
    /// Default 10, otherwise commonly 2.
    /// </summary>
    public int Base { get; init; } = 10;

    /// <summary>
    /// The following transformations are supported.
    /// "identity" (default), the transformed space is the same as the original space.
    /// "normalize", the transformed space is scaled to be between 0 and 1.
    /// </summary>
    public NumericalTransform Transform { get; init; } = NumericalTransform.Identity;

    /// <summary>
    ///     Gets the minimum value for the range
    /// </summary>
    public abstract T GetLow<T>() where T : INumber<T>, IMinMaxValue<T>;

    /// <summary>
    ///     Gets the maximum value for the range
    /// </summary>
    public abstract T GetHigh<T>() where T : INumber<T>, IMinMaxValue<T>;

    /// <summary>
    ///     Gets the minimum value for the range
    /// </summary>
    public abstract object GetLow();

    /// <summary>
    ///     Gets the maximum value for the range
    /// </summary>
    public abstract object GetHigh();

    public override ValidationResult IsValid(object value) {
        // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
        if (value == null || (value as string)?.Length == 0) {
            return ValidationResult.Success;
        }

        IComparable? convertedValue;

        try {
            convertedValue = (IComparable) Convert.ChangeType(value, DType);
        } catch (Exception e) {
            return new ValidationResult(e.Message);
        }

        var min = (IComparable) GetLow();
        var max = (IComparable) GetHigh();
        return min.CompareTo(convertedValue) <= 0 && max.CompareTo(convertedValue) >= 0 ? ValidationResult.Success : new ValidationResult("Low is greater than High");
    }
}