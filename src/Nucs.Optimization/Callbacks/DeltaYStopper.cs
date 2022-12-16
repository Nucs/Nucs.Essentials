using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Stop the optimizer if the absolute difference between the n_best objective values is less than delta.
/// </summary>
public class DeltaYStopper : PyOptCallback {
    public readonly double DeltaY;
    public readonly int NBest;

    public DeltaYStopper(PyModule skopt, double deltaY, int nBest = 5) : base(skopt, nameof(DeltaYStopper), Py.kw("delta", deltaY, "n_best", nBest)) {
        DeltaY = deltaY;
        NBest = nBest;
    }

    public DeltaYStopper(double deltaY, int nBest = 5) : this((PyModule) PyModule.Import("skopt"), deltaY, nBest) { }
}