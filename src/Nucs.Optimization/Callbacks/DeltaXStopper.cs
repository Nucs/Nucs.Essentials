using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///  	Stop the optimization when |x1 - x2| &lt; delta
/// </summary>
public class DeltaXStopper : PyOptCallback {
    public readonly double DeltaX;

    public DeltaXStopper(PyModule skopt, double deltaX) : base(skopt, nameof(DeltaXStopper), Py.kw("delta", deltaX)) {
        DeltaX = deltaX;
    }

    public DeltaXStopper(double deltaX) : this((PyModule) PyModule.Import("skopt"), deltaX) { }
}