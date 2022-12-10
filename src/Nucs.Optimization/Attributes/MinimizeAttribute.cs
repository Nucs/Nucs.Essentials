namespace Nucs.Optimization.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MinimizeAttribute : Attribute {
    public MinimizeAttribute() { }
}