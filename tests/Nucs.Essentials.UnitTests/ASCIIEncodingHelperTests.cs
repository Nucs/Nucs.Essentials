using System;
using System.Text;
using FluentAssertions;
using Microsoft.VisualBasic;
using Nucs.Extensions;
using Xunit;

namespace Nucs.Essentials.UnitTests {
    public class ASCIIEncodingHelperTests {
        [Fact]
        public void Decode() {
            var str = "hello world\n!@#$%^&*()_+";
            var bytes = Encoding.ASCII.GetBytes(str);
            str.Length.Should().Be(bytes.Length);
            Span<char> chars = stackalloc char[str.Length];
            ASCIIEncodingHelper.Decode(bytes, chars);
            new string(chars).Should().Be(str);
        }

        [Fact]
        public void DecodeToString() {
            var str = "hello world\n!@#$%^&*()_+";
            var bytes = Encoding.ASCII.GetBytes(str);
            str.Length.Should().Be(bytes.Length);
            ASCIIEncodingHelper.DecodeToString(bytes).Should().Be(str);
        }

        [Fact]
        public void Encode() {
            var str = "hello world\n!@#$%^&*()_+";
            var bytes = Encoding.ASCII.GetBytes(str);
            str.Length.Should().Be(bytes.Length);

            Span<byte> chars = stackalloc byte[str.Length];
            ASCIIEncodingHelper.Encode(str, chars);
            bytes.Should().Equal(chars.ToArray());
        }

        [Fact]
        public void EncodeAndDecode() {
            var str = "hello world\n!@#$%^&*()_+";
            Span<byte> chars = stackalloc byte[str.Length];
            Span<char> charsStr = stackalloc char[str.Length];

            ASCIIEncodingHelper.Encode(str, chars);
            ASCIIEncodingHelper.Decode(chars, charsStr);
            new string(charsStr).Should().Be(str);
        }
    }
}