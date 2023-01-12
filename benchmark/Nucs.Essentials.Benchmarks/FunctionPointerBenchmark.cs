using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Nucs.Extensions;

namespace Nucs.Essentials.Benchmarks;

/*
|                               Method |      Mean |     Error |    StdDev | Ratio | RatioSD |
|------------------------------------- |----------:|----------:|----------:|------:|--------:|
|         FunctionPointer_NoConvention | 20.907 ms | 0.4176 ms | 0.6624 ms |  1.66 |    0.04 |
|                      FunctionPointer | 20.166 ms | 0.1193 ms | 0.0996 ms |  1.60 |    0.01 |
| FunctionPointer_NoConvention_WithOpt | 17.507 ms | 0.1593 ms | 0.1490 ms |  1.39 |    0.01 |
|              FunctionPointer_WithOpt | 17.717 ms | 0.3369 ms | 0.3309 ms |  1.41 |    0.03 |
| FunctionPointer_NoConvention_AggrOpt | 17.572 ms | 0.2250 ms | 0.1995 ms |  1.39 |    0.02 |
|              FunctionPointer_AggrOpt | 17.884 ms | 0.3258 ms | 0.3878 ms |  1.42 |    0.04 |
|                                 Func | 23.205 ms | 0.3635 ms | 0.3222 ms |  1.84 |    0.03 |
|                         DelegateFunc | 23.316 ms | 0.4501 ms | 0.5183 ms |  1.86 |    0.03 |
|                   DirectCall_Inlined |  2.526 ms | 0.0266 ms | 0.0249 ms |  0.20 |    0.00 |
|                           DirectCall | 12.607 ms | 0.1014 ms | 0.0899 ms |  1.00 |    0.00 |
 */
[SimpleJob]
public class FunctionPointerBenchmark {
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int Math(int x, int y) {
        return x * y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int MathInlined(int x, int y) {
        return x * y;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MathWithOpt(int x, int y) {
        return x * y;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    private static int MathWithAggrOpt(int x, int y) {
        return x * y;
    }

    [Benchmark]
    public unsafe int FunctionPointer_NoConvention() {
        delegate*<int, int, int> input = &Math;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    public unsafe int FunctionPointer() {
        delegate*managed<int, int, int> input = &Math;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    public unsafe int FunctionPointer_NoConvention_WithOpt() {
        delegate*<int, int, int> input = &MathWithOpt;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    public unsafe int FunctionPointer_WithOpt() {
        delegate*managed<int, int, int> input = &MathWithOpt;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public unsafe int FunctionPointer_NoConvention_AggrOpt() {
        delegate*<int, int, int> input = &MathWithAggrOpt;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public unsafe int FunctionPointer_AggrOpt() {
        delegate*managed<int, int, int> input = &MathWithAggrOpt;
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    public int Func() {
        Func<int, int, int> input = new Func<int, int, int>(Math);
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }


    [Benchmark()]
    public int DelegateFunc() {
        Func<int, int, int> input = new Func<int, int, int>(Math);
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = input(j, j);
        }

        return res;
    }

    [Benchmark]
    public int DirectCall_Inlined() {
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = MathInlined(j, j);
        }

        return res;
    }

    [Benchmark(Baseline = true)]
    public int DirectCall() {
        int res = 0;
        for (int j = 1; j < 10_000_000; j++) {
            res = Math(j, j);
        }

        return res;
    }
}