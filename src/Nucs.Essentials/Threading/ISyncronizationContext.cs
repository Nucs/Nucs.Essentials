using System;
using System.Threading.Tasks;

namespace Nucs.Threading {
    public interface ISyncronizationContext : IDisposable {
        Task<T> Enqueue<T>(Func<T> act);
        Task Enqueue(Action act);
        Task<T> Enqueue<T>(Func<Task<T>> act);
        Task Enqueue(Func<Task> act);
        void EnqueueForget<T>(Func<T> act);
        void EnqueueForget(Action act);
        void EnqueueForget<T>(Func<Task<T>> act);
        void EnqueueForget(Func<Task> act);
    }
}