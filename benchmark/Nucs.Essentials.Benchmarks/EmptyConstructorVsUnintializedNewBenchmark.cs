using System;
using System.Runtime.Serialization;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|       Method |       Mean |      Error |    StdDev | Ratio | RatioSD |
|------------- |-----------:|-----------:|----------:|------:|--------:|
| Uninitalized | 1,136.9 us | 2,195.1 us | 120.32 us |  2.01 |    0.24 |
|          New |   565.3 us |   291.7 us |  15.99 us |  1.00 |    0.00 |
 */
namespace Nucs.Essentials.Benchmarks;

[ShortRunJob]
public class EmptyConstructorVsUnintializedNewBenchmark {
    [Benchmark]
    public void Uninitalized() {
        var arr = new BigMessage[10000]; 
        for (int j = 0; j < 10000; j++) {
            arr[j] = (BigMessage) FormatterServices.GetUninitializedObject(typeof(BigMessage));
        }
    }

    [Benchmark(Baseline = true)]
    public void New() {
        var arr = new BigMessage[10000]; 
        for (int j = 0; j < 10000; j++) {
            arr[j] = new BigMessage();
        }
    }

    internal class BigMessage {
        public short Prop1 { get; set; }
        public int Prop2 { get; set; }
        public long Prop3 { get; set; }
        public ushort Prop4 { get; set; }
        public uint Prop5 { get; set; }
        public ulong Prop6 { get; set; }
        public byte Prop7 { get; set; }
        public sbyte Prop8 { get; set; }
        public string Prop9 { get; set; }
        public DateTime Prop10 { get; set; }
        public TimeSpan Prop11 { get; set; }
        public bool Prop12 { get; set; }
        public char Prop13 { get; set; }
        public short[] Prop14 { get; set; }
        public int[] Prop15 { get; set; }
        public long[] Prop16 { get; set; }
        public ushort[] Prop17 { get; set; }
        public uint[] Prop18 { get; set; }
        public ulong[] Prop19 { get; set; }
        public byte[] Prop20 { get; set; }
        public sbyte[] Prop21 { get; set; }
        public string[] Prop22 { get; set; }
        public DateTime[] Prop23 { get; set; }
        public TimeSpan[] Prop24 { get; set; }
        public bool[] Prop25 { get; set; }
        public char[] Prop26 { get; set; }
        public float Prop27 { get; set; }
        public double Prop28 { get; set; }
        public decimal Prop29 { get; set; }
        public float[] Prop30 { get; set; }
        public double[] Prop31 { get; set; }
        public decimal[] Prop32 { get; set; }

        public BigMessage() { }
    }
}