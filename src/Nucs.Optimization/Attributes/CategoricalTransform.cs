namespace Nucs.Optimization.Attributes;

public enum CategoricalTransform {
    /// "identity", the transformed space is the same as the original space.
    Identity,

    /// "string", the transformed space is a string encoded representation of the original space.
    String,

    /// "label", the transformed space is a label encoded representation (integer) of the original space.
    Label,

    /// "onehot", the transformed space is a one-hot encoded representation of the original space.
    OneHot,
}