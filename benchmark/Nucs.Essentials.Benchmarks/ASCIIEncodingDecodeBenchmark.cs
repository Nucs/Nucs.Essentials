using System;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|       Method |                 Text |        Mean |       Error |    StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------- |--------------------- |------------:|------------:|----------:|------:|--------:|-------:|-------:|------:|----------:|
| NativeDecode | 1234(...)WXYZ [1054] |   486.88 ns |   223.51 ns | 12.252 ns |  1.00 |    0.00 | 0.5102 | 0.0010 |     - |    2136 B |
|   OursDecode | 1234(...)WXYZ [1054] | 1,139.07 ns | 1,051.41 ns | 57.631 ns |  2.34 |    0.14 | 0.5093 |      - |     - |    2136 B |
|              |                      |             |             |           |       |         |        |        |       |           |
| NativeDecode |         Hello World! |    32.53 ns |    28.85 ns |  1.581 ns |  1.00 |    0.00 | 0.0114 |      - |     - |      48 B |
|   OursDecode |         Hello World! |    27.15 ns |    26.56 ns |  1.456 ns |  0.84 |    0.06 | 0.0115 |      - |     - |      48 B |
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

        [GlobalSetup]
        public void Setup() {
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