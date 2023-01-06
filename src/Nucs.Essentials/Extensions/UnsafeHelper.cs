using System;
using System.Runtime.CompilerServices;

namespace Nucs.Extensions; 

internal static unsafe class UnsafeHelper {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullRef<T>(ref T source) {
        return Unsafe.AsPointer(ref source) == null;
    }

    public static bool IsNullRef(void* ptr) {
        return ptr == null;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T NullRef<T>() where T : unmanaged => ref Unsafe.AsRef<T>(IntPtr.Zero.ToPointer());
}