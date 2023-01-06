using System;
using System.Runtime.Serialization;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|                     Method |  Size |          Mean |         Error |        StdDev | Ratio | RatioSD |
|--------------------------- |------ |--------------:|--------------:|--------------:|------:|--------:|
|                   NewArray |    10 |      89.84 us |      68.80 us |      3.771 us |  1.00 |    0.00 |
|       GcUninitializedArray |    10 |      85.46 us |      41.92 us |      2.298 us |  0.95 |    0.03 |
|            GcAllocateArray |    10 |     268.88 us |     120.74 us |      6.618 us |  2.99 |    0.05 |
|             ObjectNewArray |    10 |     210.79 us |     265.25 us |     14.539 us |  2.35 |    0.23 |
| ObjectGcUninitializedArray |    10 |     194.58 us |      94.89 us |      5.201 us |  2.17 |    0.09 |
|      ObjectGcAllocateArray |    10 |     400.54 us |     170.22 us |      9.330 us |  4.46 |    0.25 |
|                            |       |               |               |               |       |         |
|                   NewArray |  1000 |   3,107.83 us |   2,091.44 us |    114.639 us |  1.00 |    0.00 |
|       GcUninitializedArray |  1000 |   2,967.37 us |     841.91 us |     46.148 us |  0.96 |    0.04 |
|            GcAllocateArray |  1000 |   3,056.39 us |   2,353.95 us |    129.028 us |  0.98 |    0.04 |
|             ObjectNewArray |  1000 |  79,609.15 us |  24,854.35 us |  1,362.351 us | 25.64 |    1.19 |
| ObjectGcUninitializedArray |  1000 |  81,227.88 us |  97,978.30 us |  5,370.521 us | 26.19 |    2.55 |
|      ObjectGcAllocateArray |  1000 |  79,933.11 us | 109,363.77 us |  5,994.597 us | 25.76 |    2.41 |
|                            |       |               |               |               |       |         |
|                   NewArray | 10000 |  50,384.46 us |  18,435.74 us |  1,010.525 us |  1.00 |    0.00 |
|       GcUninitializedArray | 10000 |  59,209.24 us |  50,085.75 us |  2,745.369 us |  1.18 |    0.07 |
|            GcAllocateArray | 10000 |  47,692.10 us |  61,991.40 us |  3,397.958 us |  0.95 |    0.06 |
|             ObjectNewArray | 10000 | 477,738.03 us | 177,768.94 us |  9,744.115 us |  9.48 |    0.15 |
| ObjectGcUninitializedArray | 10000 | 452,865.23 us | 228,666.50 us | 12,533.983 us |  8.99 |    0.21 |
|      ObjectGcAllocateArray | 10000 | 420,148.93 us | 264,103.42 us | 14,476.399 us |  8.34 |    0.33 |
 */
namespace Nucs.Essentials.Benchmarks;

[ShortRunJob]
public class NewArrayVsGcUninitializedArrayBenchmark {
    [Params(10, 1000, 10000)]
    public int Size;

    [Benchmark(Baseline = true)]
    public void NewArray() {
        var size = Size;
        var arr = new byte[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = new byte[size];
        }
    }

    [Benchmark]
    public void GcUninitializedArray() {
        var size = Size;
        var arr = new byte[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = GC.AllocateUninitializedArray<byte>(size);
        }
    }

    [Benchmark]
    public void GcAllocateArray() {
        var size = Size;
        var arr = new byte[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = GC.AllocateArray<byte>(size);
        }
    }
    
    [Benchmark]
    public void ObjectNewArray() {
        var size = Size;
        var arr = new object[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = new object[size];
        }
    }

    [Benchmark]
    public void ObjectGcUninitializedArray() {
        var size = Size;
        var arr = new object[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = GC.AllocateUninitializedArray<object>(size);
        }
    }

    [Benchmark()]
    public void ObjectGcAllocateArray() {
        var size = Size;
        var arr = new object[10000][]; 
        for (int i = 0; i < 10000; i++) {
            arr[i] = GC.AllocateArray<object>(size);
        }
    }
}