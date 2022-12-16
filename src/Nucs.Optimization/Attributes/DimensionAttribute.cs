using System.ComponentModel.DataAnnotations;

namespace Nucs.Optimization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public abstract class DimensionAttribute : Attribute {
    public abstract Type DType { get; }

    public abstract ValidationResult IsValid(object value);
}