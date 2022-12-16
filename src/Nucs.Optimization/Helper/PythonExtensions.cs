using Python.Runtime;

namespace Nucs.Optimization.Helper;

internal static class PythonExtensions {
    public static PyList ToPyList<T>(this IList<T> list) {
        var pyList = new PyList();
        foreach (var item in list) {
            pyList.Append(item.ToPython());
        }

        return pyList;
    }

    public static PyList ToPyList<T>(this IEnumerable<T> list) {
        var pyList = new PyList();
        foreach (var item in list) {
            pyList.Append(item.ToPython());
        }

        return pyList;
    }
}