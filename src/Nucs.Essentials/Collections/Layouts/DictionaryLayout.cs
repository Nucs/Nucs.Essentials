namespace Nucs.Collections.Layouts {
    public class DictionaryLayout<TKey, TValue> {
        public int[] Buckets;
        public Entry<TKey, TValue>[] Entries;
    }

    #if (NETCOREAPP3_0 || NETCOREAPP3_1)
    public struct Entry<TKey, TValue>
    {
        public int Next;
        public uint HashCode;
        public TKey Key;           // Key of entry
        public TValue Value;         // Value of entry
    }
    #else
    #if (NET5_0)
    public struct Entry<TKey, TValue> {
        public uint HashCode;
        public int Next;
        public TKey Key; // Key of entry
        public TValue Value; // Value of entry
    }
    #else
    public struct Entry<TKey, TValue> {
        public int HashCode;
        public int Next;
        public TKey Key; // Key of entry
        public TValue Value; // Value of entry
    }
    #endif
    #endif
}