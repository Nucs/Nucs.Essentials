using System;
using System.ComponentModel.DataAnnotations;

namespace Nucs.Optimization.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ValuesAttribute : ValidationAttribute {
    public readonly IReadOnlyList<object> Values;

    public ValuesAttribute(params string[] values) {
        Values = values;
    }

    public ValuesAttribute(params object[] values) {
        Values = values;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext) {
        if (Values.Contains(value)) {
            return ValidationResult.Success;
        }

        return new ValidationResult($"Value must be one of: {string.Join(", ", Values)}");
    }
}