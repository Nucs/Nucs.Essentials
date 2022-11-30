using System;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|          Method |               Input |     Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |-------------------- |---------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
| LineReaderWhile | Hell(...)asd" [109] | 3.142 us | 0.0531 us | 0.0795 us |  0.80 |    0.04 | 0.6866 |     - |     - |   2.81 KB |
|     StringSplit | Hell(...)asd" [109] | 3.936 us | 0.1204 us | 0.1803 us |  1.00 |    0.00 | 1.3962 |     - |     - |    5.7 KB |
 */
namespace Nucs.Essentials.Benchmarks;

[MediumRunJob]
[MemoryDiagnoser]
public class LineReaderParseBenchmark {
    [Params("Hello World!,123456791011125,0.33355533,\"asdasdasdasd\",Hello World!,123456791011415,0.33355533,\"asdasdasdasd\"")]
    public string Input { get; set; }

    public sealed class Poco {
        public string A;
        public long B;
        public double C;
        public string D;
        public string E;
        public long F;
        public float G;
        public string H;
    }

    [Benchmark]
    public void LineReaderWhile() {
        string input = Input;
        for (int i = 9; i >= 0; i--) {
            var lineReader = new LineReader(input);
            var poco = new Poco();
            poco.A = lineReader.Next().ToString();
            poco.B = long.Parse(lineReader.Next());
            poco.C = double.Parse(lineReader.Next());
            poco.D = lineReader.Next().ToString();
            poco.E = lineReader.Next().ToString();
            poco.F = long.Parse(lineReader.Next());
            poco.G = float.Parse(lineReader.Next());
            poco.H = lineReader.Next().ToString();
        }
    }

    [Benchmark(Baseline = true)]
    public void StringSplit() {
        string input = Input;
        for (int j = 9; j >= 0; j--) {
            var columns = input.Split(',');
            var n = 0;
            var poco = new Poco();
            poco.A = columns[n++];
            poco.B = long.Parse(columns[n++]);
            poco.C = double.Parse(columns[n++]);
            poco.D = columns[n++];
            poco.E = columns[n++];
            poco.F = long.Parse(columns[n++]);
            poco.G = float.Parse(columns[n++]);
            poco.H = columns[n++];
        }
    }
}