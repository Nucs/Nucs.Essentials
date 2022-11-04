using System;
using FluentAssertions;
using Newtonsoft.Json;
using Nucs.Collections.Structs;
using Nucs.Essentials.UnitTests.Resources;
using Nucs.Extensions;
using Xunit;

namespace Nucs.Essentials.UnitTests {
    /// <summary>
    ///     Simple stock price POCO
    /// </summary>
    public readonly struct StockInfo {
        [JsonProperty("name")]
        public readonly string Ticker;

        [JsonProperty("price")]
        public readonly double Price;

        [JsonConstructor]
        public StockInfo(string ticker, double price) {
            Ticker = ticker;
            Price = price;
        }
    }

    public class LineReaderTests {
        [Fact]
        public void ReadStringDelimited2() {
            var row = new LineReader("aaa,,aa,,aaaa,,aaaa,,aa");
            row.CountItems(",,").Should().Be(5);
            row.HasNext.Should().BeTrue();
            row.Next(",,").ToString().Should().Be("aaa");
            row.Next(",,").ToString().Should().Be("aa");
            row.Next(",,").ToString().Should().Be("aaaa");
            row.Next(",,").ToString().Should().Be("aaaa");
            row.Next(",,").ToString().Should().Be("aa");
            row.HasNext.Should().BeFalse();
            row.Next(",,").IsEmpty.Should().BeTrue();
            row.HasNext.Should().BeFalse();
        }

        [Fact]
        public void ReadStringDelimited() {
            var row = new LineReader("aaa,aa,aaaa,aaaa,aa");
            row.CountItems(",").Should().Be(5);
            row.HasNext.Should().BeTrue();
            row.Next(",").ToString().Should().Be("aaa");
            row.Next(",").ToString().Should().Be("aa");
            row.Next(",").ToString().Should().Be("aaaa");
            row.Next(",").ToString().Should().Be("aaaa");
            row.Next(",").ToString().Should().Be("aa");
            row.HasNext.Should().BeFalse();
            row.Next(",").IsEmpty.Should().BeTrue();
            row.HasNext.Should().BeFalse();
        }

        [Fact]
        public void ReadCharDelimited() {
            var row = new LineReader("aaa,aa,aaaa,aaaa,aa");
            row.CountItems(',').Should().Be(5);
            row.HasNext.Should().BeTrue();
            row.Next(',').ToString().Should().Be("aaa");
            row.Next(',').ToString().Should().Be("aa");
            row.Next(',').ToString().Should().Be("aaaa");
            row.Next(',').ToString().Should().Be("aaaa");
            row.Next(',').ToString().Should().Be("aa");
            row.HasNext.Should().BeFalse();
            row.Next(',').IsEmpty.Should().BeTrue();
            row.HasNext.Should().BeFalse();
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiter() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\r\na,a,a,a,a");
            rowReader.ResolvedDelimiter.Should().Be(RowReader.LineDelimiter.CRLF);
            var row = new LineReader(rowReader.Next());

            row.CountItems(',').Should().Be(5);
            row.HasNext.Should().BeTrue();
            row.Next(',').ToString().Should().Be("aaa");
            row.Next(',').ToString().Should().Be("aa");
            row.Next(',').ToString().Should().Be("aaaa");
            row.Next(',').ToString().Should().Be("aaaa");
            row.Next(',').ToString().Should().Be("aa");
            row.HasNext.Should().BeFalse();
            row.Next(',').IsEmpty.Should().BeTrue();
            row.HasNext.Should().BeFalse();

            rowReader.HasNext.Should().BeTrue();

            row = new LineReader(rowReader.Next());
            row.CountItems(',').Should().Be(5);
            row.HasNext.Should().BeTrue();
            row.Next(',').ToString().Should().Be("a");
            row.Next(',').ToString().Should().Be("a");
            row.Next(',').ToString().Should().Be("a");
            row.Next(',').ToString().Should().Be("a");
            row.Next(',').ToString().Should().Be("a");
            row.HasNext.Should().BeFalse();
            row.Next(',').IsEmpty.Should().BeTrue();
            row.HasNext.Should().BeFalse();
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiterCleaner() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\r\na,a,a,a,a");
            int read = 0;
            while (rowReader.HasNext) {
                var rowText = rowReader.Next();
                rowText.ToString().Should().NotContain("\r").And.NotContain("\n");
                var row = new LineReader(rowText);
                row.CountItems(',').Should().Be(5);
                row.Next().ToString().Should().NotContain("\r").And.NotContain("\n");
                //do something with row
                read++;
            }

            read.Should().Be(2);
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiterCleanerWithCF() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\na,a,a,a,a");
            int read = 0;
            while (rowReader.HasNext) {
                var rowText = rowReader.Next();
                rowText.ToString().Should().NotContain("\r").And.NotContain("\n");
                var row = new LineReader(rowText);
                row.CountItems(',').Should().Be(5);
                row.Next().ToString().Should().NotContain("\r").And.NotContain("\n");
                //do something with row
                read++;
            }

            read.Should().Be(2);
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiterCleanerWithEmpty() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\r\na,a,a,a,a\r\n");
            int read = 0;
            while (rowReader.HasNext) {
                var rowText = rowReader.Next();
                rowText.ToString().Should().NotContain("\r").And.NotContain("\n");
                var row = new LineReader(rowText);
                row.CountItems(',').Should().Be(5);
                row.Next().ToString().Should().NotContain("\r").And.NotContain("\n");
                //do something with row
                read++;
            }

            read.Should().Be(2);
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiterCleanerSkippingFirst() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\r\na,a,a,a,a\r\n");
            int read = 0;
            rowReader.Skip(1); //skip header
            while (rowReader.HasNext) {
                var rowText = rowReader.Next();
                rowText.ToString().Should().NotContain("\r").And.NotContain("\n");
                var row = new LineReader(rowText);
                row.CountItems(',').Should().Be(5);
                row.Next().ToString().Should().NotContain("\r").And.NotContain("\n");
                //do something with row
                read++;
            }

            read.Should().Be(1);
        }

        [Fact]
        public void ReadCharDelimitedWithRowDelimiterCleanerWithBadEndRow() {
            var rowReader = new RowReader("aaa,aa,aaaa,aaaa,aa\r\na,a,a,a,a\r");
            int read = 0;
            while (rowReader.HasNext) {
                var rowText = rowReader.Next();
                rowText.ToString().Should().NotContain("\n");
                var row = new LineReader(rowText);
                row.CountItems(',').Should().Be(5);
                row.Next().ToString().Should().NotContain("\n");
                //do something with row
                read++;
            }

            read.Should().Be(2);
        }


        [Fact]
        public void ReadCsvLines() {
            var sourceContent = EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv");
            var lines = sourceContent.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var dataset = new StructList<StockInfo>(lines.Length - 1 /*skip header*/);
            new LineReader(lines[0]).CountItems(",").Should().Be(4);
            var row = new LineReader("aaa,,aa,,aaaa,,aaaa,,aa");
            new LineReader(lines[0]).CountItems(',').Should().Be(4);

            for (int i = 1 /*skip header*/; i < lines.Length; i++) {
                var lineReader = new LineReader(lines[i]);
                var name = lineReader.Next().ToString();
                lineReader.Skip(1);
                var price = double.Parse(lineReader.Next());
                dataset.Add(new StockInfo(name, price));
            }

            dataset.Should().HaveCount(3200);
        }

        [Fact]
        public void ReadCsvLinesWithRowReader() {
            var sourceContent = EmbeddedResourceHelper.ReadEmbeddedResource("stocks.csv");
            var lines = sourceContent.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var dataset = new StructList<StockInfo>(lines.Length - 1 /*skip header*/);

            for (int i = 1 /*skip header*/; i < lines.Length; i++) {
                var lineReader = new LineReader(lines[i]);
                var name = lineReader.Next().ToString();
                lineReader.Skip(1);
                var price = double.Parse(lineReader.Next());
                dataset.Add(new StockInfo(name, price));
            }

            dataset.Should().HaveCount(3200);
        }


        private static StructList<StockInfo> ReadCsvFile(string[] sourceContent) {
            var dataset = new StructList<StockInfo>(sourceContent.Length - 1 /*skip header*/);

            for (int i = 1 /*skip header*/; i < sourceContent.Length; i++) {
                var lineReader = new LineReader(sourceContent[i]);
                var name = lineReader.Next().ToString();
                lineReader.Skip(1);
                var price = double.Parse(lineReader.Next());
                dataset.Add(new(name, price));
            }

            return dataset;
        }

        /*private static StructList<StockInfo> ReadCsvFile(string sourceContent) {
            var dataset = new StructList<StockInfo>(sourceContent.Length - 1 /*skip header#1#);

            for (int i = 1 /*skip header#1#; i < sourceContent.Length; i++) {
                var lineReader = new LineReader(sourceContent[i]);
                var name = lineReader.Next().ToString();
                lineReader.Skip(1);
                var price = double.Parse(lineReader.Next());
                dataset.Add(new(name, price));
            }

            return dataset;
        }*/
    }
}