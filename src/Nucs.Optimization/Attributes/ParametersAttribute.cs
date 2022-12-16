namespace Nucs.Optimization.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class ParametersAttribute : Attribute {
    public ParametersInclusion Inclusion { get; init; } = ParametersInclusion.ImplicitAndExplicit;
    public ParametersAttribute() { }
}