using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Stop the optimization before running out of a fixed budget of time.
/// </summary>
public class DeadlineStopper : PyOptCallback {
    public readonly TimeSpan Deadline;

    public DeadlineStopper(PyModule skopt, TimeSpan deadline) : base(skopt, nameof(DeadlineStopper), Py.kw("total_time", deadline.TotalSeconds)) {
        Deadline = deadline;
    }

    public DeadlineStopper(TimeSpan deadline) : this((PyModule) PyModule.Import("skopt"), deadline) { }
}