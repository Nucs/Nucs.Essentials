using System;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|       Method |                 Text |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------- |--------------------- |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
| NativeEncode | 1234(...)WXYZ [1054] | 121.39 ns | 254.34 ns | 13.941 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   OursEncode | 1234(...)WXYZ [1054] | 642.31 ns | 494.52 ns | 27.107 ns |  5.32 |    0.41 |     - |     - |     - |         - |
|              |                      |           |           |           |       |         |       |       |       |           |
| NativeEncode |         Hello World! |  15.37 ns |  23.59 ns |  1.293 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|   OursEncode |         Hello World! |  14.67 ns |  22.68 ns |  1.243 ns |  0.96 |    0.15 |     - |     - |     - |         - |
 */
namespace Nucs.Essentials.Benchmarks {
    [ShortRunJob]
    [MemoryDiagnoser]
    public class ASCIIEncodingDecodeBenchmark {
        [Params(
            /*short string*/
            "Hello World!",
            /*long string*/
            "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        public string Text { get; set; }
        public byte[] TextBytes { get; set; }

        public ASCIIEncodingDecodeBenchmark() {
            TextBytes = Encoding.ASCII.GetBytes(Text);
        }

        [Benchmark(Baseline = true)]
        public string NativeDecode() {
            return Encoding.ASCII.GetString(TextBytes);
        }

        [Benchmark]
        public string OursDecode() {
            return ASCIIEncodingHelper.DecodeToString(TextBytes);
        }
    }
}