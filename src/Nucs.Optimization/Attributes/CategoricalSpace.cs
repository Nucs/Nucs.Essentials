using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Nucs.Optimization.Attributes;

/// <summary>
/// Search space dimension that can take on categorical values.
/// </summary>
/// <remarks>https://scikit-optimize.github.io/stable/_modules/skopt/space/space.html</remarks>
public abstract class CategoricalSpace : DimensionAttribute {
    /// <summary>
    /// Sequence of possible categories.
    /// </summary>
    public abstract object[] ObjectCategories { get; }

    /// <summary>
    /// Prior probabilities for each category. By default all categories are equally likely.
    /// </summary>
    public double[]? Prior { get; init; }

    /// <summary>
    /// The following transformations are supported.
    /// "identity", the transformed space is the same as the original space.
    /// "string", the transformed space is a string encoded representation of the original space.
    /// "label", the transformed space is a label encoded representation (integer) of the original space.
    /// "onehot", the transformed space is a one-hot encoded representation of the original space.
    /// "function", the transformation is handled by <see name="TransformFunction"/>
    /// </summary>
    public CategoricalTransform Transform { get; init; } = CategoricalTransform.OneHot;
}

/// <summary>
/// Search space dimension that can take on categorical values.
/// </summary>
/// <remarks>https://scikit-optimize.github.io/stable/_modules/skopt/space/space.html</remarks>
public sealed class CategoricalSpace<T> : CategoricalSpace {
    /// <summary>
    /// Sequence of possible categories.
    /// </summary>
    public T[] Categories { get; init; }

    public CategoricalSpace() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoricalSpace"/> class.
    /// </summary>
    /// <param name="categories">Sequence of possible categories.</param>
    public CategoricalSpace(params T[]? categories) {
        if (categories?.Length > 0)
            this.Categories = categories;
        else {
            if (IsEnum) {
                this.Categories = Enums.GetValues(typeof(T)).Select(o => (T) o).ToArray();
            } else {
                throw new InvalidOperationException("No categories were specified and the type is not an enum.");
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoricalSpace"/> class.
    /// </summary>
    /// <param name="categories">Sequence of possible categories.</param>
    public CategoricalSpace(params object[]? categories)
        : this(IsEnum
                   ? categories?.Select(t => (T) Enums.Parse(typeof(T), t as string ?? t?.ToString() ?? throw new NullReferenceException($""))).ToArray()
                   : categories?.Select(t => (T) Convert.ChangeType(t, typeof(T), CultureInfo.InvariantCulture)).ToArray()) { }

    public static readonly bool IsEnum = typeof(T).IsEnum;

    public override Type DType => typeof(T);

    public override object[] ObjectCategories => IsEnum
        ? (object[]) Categories.Select(t => (object) t.ToString()).ToArray()
        : typeof(T) == typeof(bool)
            ? new object[] { true, false }
            : (object[]) Categories.Select(t => (object) t).ToArray();

    public override ValidationResult IsValid(object value) {
        foreach (var category in Categories) {
            if (category.Equals(value))
                return ValidationResult.Success;
        }

        return new ValidationResult($"Value must be one of: {string.Join(", ", Categories)}");
    }
}