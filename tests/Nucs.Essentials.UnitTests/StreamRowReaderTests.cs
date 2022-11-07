using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Nucs.Essentials.UnitTests.Resources;
using Nucs.Extensions;
using Nucs.Text;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests {
    public class StreamRowReaderTests {
        private readonly ITestOutputHelper _cw;

        public StreamRowReaderTests(ITestOutputHelper cw) {
            _cw = cw;
        }

        [Fact]
        public void ReadEntirely() {
            //action
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv")!));
            using var reader = new StreamRowReader(ms, LineDelimiter.CRLF, bufferSize: 25, splitOptions: StringSplitOptions.RemoveEmptyEntries);
            ReadOnlySpan<char> read = default;
            int i = 0;
            while (reader.HasNext) {
                read = reader.Next();
                _cw.WriteLine(read.ToString());
            }

            _cw.WriteLine(i.ToString());
            //test last
            read.ToString().Should().Be("ZYNE,17-Dec-18,4.1,298700");
        }

        [Fact]
        public void ReadEntirelyAndParse() {
            //action
            var sw = Stopwatch.StartNew();
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv")!));
            using var reader = new StreamRowReader(ms, LineDelimiter.CRLF, bufferSize: 25, splitOptions: StringSplitOptions.RemoveEmptyEntries);
            ReadOnlySpan<char> read = default;
            reader.Skip(1); //skip header
            int i = 0;
            while (reader.HasNext) {
                read = reader.Next();
                /*
                 * Symbol,Date,Price,Volume
                 * AABA,17-Dec-18,60.08,8897800
                 */
                var lineReader = new LineReader(read);
                lineReader.Next().ToString().Should().Match(r => r.All(char.IsLetter));
                lineReader.Next().ToString().Substring(0, 1).Should().Match(c => char.IsDigit(c[0]));
                double.Parse(lineReader.Next());
                int.Parse(lineReader.Next());
                lineReader.HasNext.Should().BeFalse();
                i++;
 
                //_cw.WriteLine(read.ToString());
            }

            _cw.WriteLine(i.ToString());
            //test last
            read.ToString().Should().Be("ZYNE,17-Dec-18,4.1,298700");
            _cw.WriteLine($"[{sw.Elapsed:hh\\:mm\\:ss}] Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns;");
        }

        [Fact(Skip = "Benchmark only")]
        public void ReadEntirelyBenchmark() {
            //start
            var bytes = Encoding.UTF8.GetBytes(EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv")!);
            var sw = Stopwatch.StartNew();
            int i = 0;

            ReadOnlySpan<char> read = default;
            for (int j = 0; j < 100000; j++) {
                //action
                using var ms = new MemoryStream(bytes);
                using var reader = new StreamRowReader(ms, LineDelimiter.CRLF, bufferSize: 8192, splitOptions: StringSplitOptions.RemoveEmptyEntries);
                while (reader.HasNext) {
                    read = reader.Next().Trim();
                    i++;
                }

                //read.ToString().Should().Be("ZYNE,17-Dec-18,4.1,298700");
                if (j % 1000 == 0)
                    _cw.WriteLine($"Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns;");
            }

            _cw.WriteLine($"Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns;");
            _cw.WriteLine(read.ToString());
        }

        [Fact(Skip = "Benchmark only")]
        public void ReadEntirelyViaSplitBenchmark() {
            //start
            var bytes = Encoding.UTF8.GetBytes(EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv")!);
            var sw = Stopwatch.StartNew();
            int i = 0;
            string line = "";
            for (int j = 0; j < 100000; j++) {
                //action
                var str = Encoding.UTF8.GetString(bytes);
                var split = str.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                for (int k = 0; k < split.Length; k++) {
                    line = split[k].Trim();

                    i++;
                }

                //read.ToString().Should().Be("ZYNE,17-Dec-18,4.1,298700");
                if (j % 1000 == 0)
                    _cw.WriteLine($"Iterated {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns;");
            }

            _cw.WriteLine($"Iterated {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns;");
            _cw.WriteLine(line.ToString());
        }

        [Fact]
        public void ReadEntirelyAndTestContent() {
            //prepare
            /*var rows = new[] {
                "Symbol,Date,Price,Volume",
                "AABA,17-Dec-18,60.08,8897800",
                "AAL,17-Dec-18,32.04,7638100",
                "AAME,17-Dec-18,2.42,8800",
                "AAOI,17-Dec-18,17.11,838400",
                "AAON,17-Dec-18,34.28,156100",
                "AAPL,17-Dec-18,163.94,44287900",
                "AAWW,17-Dec-18,42.63,417900",
                "AAXJ,17-Dec-18,64.47,2614700",
                "AAXN,17-Dec-18,43.8,622000",
                "ABAC,17-Dec-18,1.03,6400",
                "ABCB,17-Dec-18,31.18,1570900"
            };*/
            var data = EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv")!;
            var rows = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var reader = new StreamRowReader(ms, LineDelimiter.CRLF);

            //action and test
            for (int i = 0; i < rows.Length; i++) {
                var returned = reader.Next().ToString();
                returned.Should().Be(rows[i]);
            }
        }

        [Theory]
        [InlineData(10, "11")]
        [InlineData(10, "hello123123")]
        [InlineData(10, "hello123123\nhello123123")]
        [InlineData(10, "hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123")]
        [InlineData(10000, "11")]
        [InlineData(10000, "hello123123")]
        [InlineData(10000, "hello123123\nhello123123")]
        [InlineData(10000, "hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123hello123123")]
        public void AppendStringFromBytes(int bufferSize, string payload) {
            //prepare
            var bytes = Encoding.UTF8.GetBytes(payload);
            var sb = new ValueStringBuilder(bufferSize);

            //test external allocation
            ArrayPool<char>.Shared.Rent(bufferSize).Should()
                           .NotBeNull()
                           .And.HaveCountGreaterOrEqualTo(bufferSize);

            //test internal allocation
            sb.Capacity.Should().NotBe(0);
            sb.RawChars.Length.Should().BeGreaterOrEqualTo(bufferSize);

            //action
            int chars = sb.Append(bytes, Encoding.UTF8);
            chars.Should().Be(payload.Length);

            //test
            sb.AsSpan().ToString().Should().Be(payload);
            sb.RemoveStart(2);
            sb.AsSpan().ToString().Should().Be(payload.AsSpan(2).ToString());

            //action
            sb.Clear();

            //test
            sb.Capacity.Should().NotBe(0);
        }

        [Fact]
        public void AppendOnSmallBuffer() {
            var bytes = Encoding.UTF8.GetBytes("12345123451234512345");

            var sb = new ValueStringBuilder(10);
            sb.Capacity.Should().NotBe(0);
            sb.RawChars.Length.Should().BeGreaterOrEqualTo(10);

            int chars = sb.Append(bytes, Encoding.UTF8);

            chars.Should().Be("12345123451234512345".Length);
            sb.ToString().Should().Be("12345123451234512345");
        }
    }
}