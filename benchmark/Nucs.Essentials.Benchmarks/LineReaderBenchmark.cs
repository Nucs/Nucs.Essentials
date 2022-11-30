using System;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using Nucs.Extensions;


/*
|          Method |                Input |       Mean |      Error |     StdDev | Ratio | RatioSD |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------- |--------------------- |-----------:|-----------:|-----------:|------:|--------:|---------:|------:|------:|----------:|
| LineReaderWhile |  Hell(...)asd" [119] |   7.174 us |   6.396 us |  0.3506 us |  0.32 |    0.03 |        - |     - |     - |         - |
|     StringSplit |  Hell(...)asd" [119] |  22.361 us |  11.675 us |  0.6400 us |  1.00 |    0.00 |  12.4207 |     - |     - |   52000 B |
|                 |                      |            |            |            |       |         |          |       |       |           |
| LineReaderWhile | Hell(...)asd" [4799] | 282.391 us | 196.780 us | 10.7862 us |  0.35 |    0.01 |        - |     - |     - |         - |
|     StringSplit | Hell(...)asd" [4799] | 807.034 us |  95.692 us |  5.2452 us |  1.00 |    0.00 | 474.6094 |     - |     - | 1986401 B |
 */
namespace Nucs.Essentials.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class LineReaderBenchmark {
    [Params("Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\"",
            /*Long row*/ "Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\",Hello World!,12345679101112131415,0.33355533,\"asdasdasdasd\"")]
    public string Input;

    [Benchmark]
    public void LineReaderWhile() {
        var input = Input;
        for (int j = 0; j < 100; j++) {
            var lineReader = new LineReader(input);
            while (lineReader.HasNext) {
                var column = lineReader.Next().Trim();
            }
        }
    }


    [Benchmark(Baseline = true)]
    public void StringSplit() {
        var input = Input;
        for (int j = 0; j < 100; j++) {
            var columns = input.Split(',', StringSplitOptions.TrimEntries);
            for (int i = columns.Length - 1; i >= 0; i--) {
                var split = columns[i];
            }
        }
    }
}