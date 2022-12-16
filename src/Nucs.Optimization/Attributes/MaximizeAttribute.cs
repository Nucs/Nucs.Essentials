namespace Nucs.Optimization.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MaximizeAttribute : Attribute {
    public MaximizeAttribute() { }
}