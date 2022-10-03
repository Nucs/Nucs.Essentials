using System;
using System.Threading.Tasks;

namespace Nucs.Timing.Bottlenecking {
    public readonly struct CallTarget {
        public readonly Func<object> Function;
        public readonly TaskCompletionSource<object> TaskSource;

        public CallTarget(TaskCompletionSource<object> taskSource, Func<object> function) {
            Function = function ?? throw new ArgumentNullException(nameof(function));
            TaskSource = taskSource;
        }

        public CallTarget(Func<object> function) {
            Function = function;
            TaskSource = new TaskCompletionSource<object>();
        }

        public Task<T> GetTask<T>() {
            return TaskSource.Task.ContinueWith(task => (T) task.Result);
        }

        public async Task<T> GetUnboxedTask<T>() {
            return await ((Task<T>) await TaskSource.Task.ConfigureAwait(false)).ConfigureAwait(false);
        }

        public async Task GetUnboxedTask() {
            await ((Task) await TaskSource.Task.ConfigureAwait(false)).ConfigureAwait(false);
        }

        public object Invoke(bool swallow = false) {
            try {
                var result = Function();
                TaskSource?.SetResult(result);
                return result;
            } catch (Exception e) {
                if (TaskSource == null && !swallow)
                    throw;

                TaskSource.SetException(e);
                return e;
            }
        }
    }
}