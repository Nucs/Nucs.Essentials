using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Callback to control the verbosity.
/// </summary>
public class VerboseCallback : PyOptCallback {
    public readonly int NInit;
    public readonly int NRandom;
    public readonly int NTotal;

    public VerboseCallback(PyModule skopt, int nInit, int nRandom, int nTotal) : base(skopt, nameof(VerboseCallback), Py.kw("n_init", nInit, "n_random", nRandom, "n_total", nTotal)) {
        NInit = nInit;
        NRandom = nRandom;
        NTotal = nTotal;
    }

    public VerboseCallback(int nInit, int nRandom, int nTotal) : this((PyModule) PyModule.Import("skopt"), nInit, nRandom, nTotal) { }
}