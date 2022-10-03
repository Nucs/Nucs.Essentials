using System.Runtime.CompilerServices;

namespace Nucs.Collections.Layouts {
    public class ArrayLayout {
        public uint Length;
        public uint Padding;
        public byte Data;
    }

    public static class ArrayLayoutExtensions {
        public static ref T GetArrayDataReference<T>(this T[] array) =>
            ref Unsafe.As<byte, T>(ref Unsafe.As<ArrayLayout>(array).Data);

        public static ArrayLayout AsLayout<T>(this T[] array) =>
            Unsafe.As<T[], ArrayLayout>(ref array);

        public static ref ArrayLayout AsLayout<T>(ref T[] array) =>
            ref Unsafe.As<T[], ArrayLayout>(ref array);
    }
}