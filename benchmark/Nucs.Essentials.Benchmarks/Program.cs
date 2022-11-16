using BenchmarkDotNet.Running;

namespace Nucs.Essentials.Benchmarks {
    public class Program {
        public static void Main(string[] args) {
            var summary = BenchmarkRunner.Run<ASCIIEncodingEncodeBenchmark>();
        }
    }
}