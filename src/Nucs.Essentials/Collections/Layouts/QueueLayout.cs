// ReSharper disable InconsistentNaming
// ReSharper disable UnassignedField.Global

using System.Runtime.CompilerServices;

namespace Nucs.Collections.Layouts;

//WHAT IF WE CAST TO A LAYOUT that is T : struct and have SUPER OPTIMIZED METHODS
public class QueueLayout<T> {
    public T[] _items;
    public int _head; // The index from which to dequeue if the queue isn't empty.
    public int _tail; // The index at which to enqueue if the queue isn't full.
    public int _size; // Number of elements.
    public int _version;

#if !(NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET6_0 || NET7_0)
        #pragma warning disable 649
        public Object SyncRoot;
        #pragma warning restore 649
#endif

    public ref T this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref _items[(_head + index) % _items.Length];
    }

    public ref T GetPinnableReference() {
        return ref _items[0];
    }
}

public static class QueueLayoutTests {
    public static void MyTest() {
        ;
    }
}