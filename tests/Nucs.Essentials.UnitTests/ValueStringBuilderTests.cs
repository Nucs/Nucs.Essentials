using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using FluentAssertions;
using Nucs.Text;
using Xunit;

namespace Nucs.Essentials.UnitTests {
    public class ValueStringBuilderTests {
        [Fact]
        public void RemoveStart() {
            using var sb = new ValueStringBuilder(10);
            sb.Append("abcd");
            sb.AsSpan().ToString().Should().Be("abcd");

            sb.RemoveStart(2);
            sb.AsSpan().ToString().Should().Be("cd");
        }

        [Fact]
        public void RemoveStartEmpty() {
            using var sb = new ValueStringBuilder(10);
            sb.Append("abcd");
            sb.AsSpan().ToString().Should().Be("abcd");

            sb.RemoveStart(4);
            sb.Length.Should().Be(0);
            sb.AsSpan().ToString().Should().Be("");
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
            using var sb = new ValueStringBuilder(bufferSize);

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

            using var sb = new ValueStringBuilder(10);
            sb.Capacity.Should().NotBe(0);
            sb.RawChars.Length.Should().BeGreaterOrEqualTo(10);

            int chars = sb.Append(bytes, Encoding.UTF8);

            chars.Should().Be("12345123451234512345".Length);
        }
    }
}