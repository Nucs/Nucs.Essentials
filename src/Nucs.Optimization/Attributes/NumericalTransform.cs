namespace Nucs.Optimization.Attributes;

public enum NumericalTransform {
    /// "identity" (default), the transformed space is the same as the original space.
    Identity,

    /// "normalize", the transformed space is scaled to be between 0 and 1.
    Normalize,
}