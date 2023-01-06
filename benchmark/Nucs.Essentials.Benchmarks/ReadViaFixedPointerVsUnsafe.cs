using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Collections.Layouts;
using Nucs.Extensions;


/*
|                       Method |     Mean |    Error |   StdDev | Ratio | RatioSD |
|----------------------------- |---------:|---------:|---------:|------:|--------:|
|                 FixedPointer | 10.54 ms | 0.193 ms | 0.171 ms |  1.00 |    0.00 |
|                    Unsafe_As | 10.25 ms | 0.074 ms | 0.069 ms |  0.97 |    0.02 |
|               Unsafe_Span_As | 10.17 ms | 0.046 ms | 0.039 ms |  0.96 |    0.02 |
| Unsafe_GetArrayDataReference | 10.16 ms | 0.090 ms | 0.084 ms |  0.96 |    0.02 |

 */
namespace Nucs.Essentials.Benchmarks;

public unsafe class ReadViaFixedPointerVsUnsafe {
    //data from 1 to 100
    private double[] _values;
    private byte[] _data;

    [GlobalSetup]
    public void Setup() {
        _values = Enumerable.Range(0, 100).Select(s => (double) s).ToArray();
        _data = new byte[_values.Length * sizeof(double)];
        fixed (double* ptr = _values) {
            Marshal.Copy((IntPtr) ptr, _data, 0, _data.Length);
        }
    }

    [Benchmark(Baseline = true)]
    public double FixedPointer() {
        var data = _data;
        double output = 0;
        for (int j = 0; j < 100000; j++) {
            for (int i = 0; i < 100; i++) {
                fixed (void* pbyte = &data[i * sizeof(double)]) {
                    output += *((double*) pbyte);
                }
            }
        }

        return output;
    }

    [Benchmark]
    public double Unsafe_As() {
        var data = _data;
        double output = 0;
        for (int j = 0; j < 100000; j++) {
            for (int i = 0; i < 100; i++) {
                output += Unsafe.As<byte, double>(ref data[i * sizeof(double)]);
            }
        }

        return output;
    }

    [Benchmark]
    public double Unsafe_Span_As() {
        var data = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_data), _data.Length);
        double output = 0;
        for (int j = 0; j < 100000; j++) {
            for (int i = 0; i < 100; i++) {
                output += Unsafe.As<byte, double>(ref data[i * sizeof(double)]);
            }
        }

        return output;
    }

    [Benchmark]
    public double Unsafe_GetArrayDataReference() {
        var data = _data;
        double output = 0;
        for (int j = 0; j < 100000; j++) {
            for (int i = 0; i < 100; i++) {
                output += Unsafe.Add<double>(ref Unsafe.As<byte, double>(ref MemoryMarshal.GetArrayDataReference(data)), i);
            }
        }

        return output;
    }
}