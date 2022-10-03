using System;

namespace Nucs.Events {
    public interface IDelegate {
        public Type Type { get; }
        public int Subscribe(Delegate act);

        public bool Unsubscribe(int token);

        public void Clear();

        public void Invoke<T>(object sender, T payload);
        public void Invoke<T>(object sender, ref T payload);
    }
}