namespace Nucs.Optimization.Attributes;

public enum ParametersInclusion {
    /// <summary>
    ///     Only parameters decorated with <see cref="IntegerSpace{T}"/> or <see cref="RealSpace{T}"/> or <see cref="CategoricalSpace{T}"/> will be included.
    /// </summary>
    ExplicitOnly,

    /// <summary>
    ///     (default) Decorated parameters/fields will be included and non decorated will be infered.
    /// </summary>
    ImplicitAndExplicit,
}