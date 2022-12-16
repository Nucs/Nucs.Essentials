using Python.Runtime;

namespace Nucs.Optimization.Callbacks;

/// <summary>
///     Save current state after each iteration with skopt.dump.
/// </summary>
public class CheckpointSaver : PyOptCallback {
    public readonly FileInfo CheckpointPath;

    public CheckpointSaver(PyModule skopt, FileInfo checkpointPath) : base(skopt, nameof(CheckpointSaver), Py.kw("checkpoint_path", checkpointPath.FullName)) {
        CheckpointPath = checkpointPath;
    }

    public CheckpointSaver(FileInfo checkpointPath) : this((PyModule) PyModule.Import("skopt"), checkpointPath) { }
}