namespace Nucs.Optimization.Attributes;

/// <summary>
/// Distribution to use when sampling random integers for this dimension.
/// If "uniform", integers are sampled uniformly between the lower and upper bounds.
/// If "log-uniform", integers are sampled uniformly between log(lower, base) and log(upper, base) where log has base base.
/// </summary>
public enum Prior {
    /// <summary>
    ///     If "uniform", integers are sampled uniformly between the lower and upper bounds.
    /// </summary>
    Uniform,

    /// <summary>
    ///     If "log-uniform", integers are sampled uniformly between log(lower, base) and log(upper, base) where log has base base.
    /// </summary>
    LogUniform,
}