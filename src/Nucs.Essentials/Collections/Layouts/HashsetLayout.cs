namespace Nucs.Collections.Layouts {
    public class HashsetLayout<T> {
        public int[] Buckets;
        public Slot<T>[] Entries;
        public object Object1;
        public object Object2;
        public int Count { get; }
    }

    public struct Slot<T> {
        public int HashCode;
        public int Next;
        public T Value;
    }
}