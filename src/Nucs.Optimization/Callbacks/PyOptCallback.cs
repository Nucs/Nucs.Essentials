using System.Globalization;
using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

public abstract class PyOptCallback : IDisposable {
    public PyObject This;

    protected PyOptCallback(PyModule skopt, string name, Py.KeywordArguments kwargs) {
        This = skopt.Get("callbacks").GetAttr(name).Invoke(Array.Empty<PyObject>(), kwargs);
    }

    protected PyOptCallback(PyObject @this) {
        This = @this;
    }

    protected PyOptCallback() { }

    public void Dispose() {
        This?.Dispose();
    }
}