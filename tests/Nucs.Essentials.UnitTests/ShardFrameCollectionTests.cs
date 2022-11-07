using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Nucs.Collections;
using Nucs.Collections.Structs;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests {
    public unsafe class ShardFrameCollectionTests {
        private readonly ITestOutputHelper _testOutputHelper;

        public ShardFrameCollectionTests(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ShardAddAndIndexer() {
            var shard = new ShardFrameCollection(128, 100);
            shard.Add(Payload(size: 3, value: 1));
            shard.Add(Payload(size: 3, value: 2));

            var item1 = shard[0];
            var item2 = shard[1];

            item1.ToArray().Should().BeEquivalentTo(Payload(size: 3, value: 1));
            item2.ToArray().Should().BeEquivalentTo(Payload(size: 3, value: 2));
        }

        [Fact]
        public void ShardAddAndIndexerFor20000Items() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 100);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardAddAndIndexerWith1BucketSize() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 1);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardGetRangeBucket10() {
            var shard = new ShardFrameCollection(50, bucketSize: 10);
            shard.Add("A");
            shard.Add("B");
            shard.Add("C");
            shard.Add("D");

            var expected = new List<byte> { 1, 0, 0, 0, (byte) 'A', 1, 0, 0, 0, (byte) 'B', 1, 0, 0, 0, (byte) 'C', 1, 0, 0, 0, (byte) 'D' };
            expected.Count.Should().Be((int) shard.BytesLength);
            for (int i = 0; i < expected.Count; i++) {
                shard.Buffer[i].Should().Be(expected[i]);
            }

            var data = shard.GetRange(startIndex: 1, count: 2);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'B', 1, 0, 0, 0, (byte) 'C' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 1, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'B' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 3, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'D' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 0, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'A' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }
        }

        [Fact]
        public void ShardGetRangeBucket1() {
            var shard = new ShardFrameCollection(50, bucketSize: 1);
            shard.Add("A");
            shard.Add("B");
            shard.Add("C");
            shard.Add("D");

            var expected = new List<byte> { 1, 0, 0, 0, (byte) 'A', 1, 0, 0, 0, (byte) 'B', 1, 0, 0, 0, (byte) 'C', 1, 0, 0, 0, (byte) 'D' };
            expected.Count.Should().Be((int) shard.BytesLength);
            for (int i = 0; i < expected.Count; i++) {
                shard.Buffer[i].Should().Be(expected[i]);
            }

            var data = shard.GetRange(startIndex: 1, count: 2);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'B', 1, 0, 0, 0, (byte) 'C' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 1, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'B' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 3, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'D' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }

            data = shard.GetRange(startIndex: 0, count: 1);
            expected = new List<byte> { 1, 0, 0, 0, (byte) 'A' };
            expected.Count.Should().Be((int) data.Length);

            for (int i = 0; i < expected.Count; i++) {
                data[i].Should().Be(expected[i]);
            }
        }

        [Fact]
        public void ShardAddAndResize() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 1, true, 1.1f);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardAddAndResizeFraction() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 1, true, 1.01f);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardAddAndIndexerFor20000ItemsIterate() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 100);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            StrongBox<int> steps = new StrongBox<int>(0);
            shard.Iterate(0, 20000, (index, frame) => {
                shard[index].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (index + 1))));
                steps.Value++;
            });

            steps.Value.Should().Be(20000);
        }

        [Fact]
        public void ShardAddAndIndexerNonX10BucketSize() {
            var shard = new ShardFrameCollection(140_000, bucketSize: 128);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            StrongBox<int> steps = new StrongBox<int>(0);
            shard.Iterate(0, 20000, (index, frame) => {
                shard[index].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (index + 1))));
                steps.Value++;
            });

            steps.Value.Should().Be(20000);
        }

        [Fact]
        public void StringFrames() {
            var shard = new ShardFrameCollection(10, bucketSize: 100, supportBufferExpansion: true, bufferExpansionFactor: 1.1f);

            for (int i = 0; i < 10000; i++) {
                shard.Add("Hello" + i);
                Encoding.UTF8.GetString(shard[i].TryToSpan()).Should().Be("Hello" + i);
            }
        }

        [Fact]
        public void StringFramesNonRoundBucketSize() {
            var shard = new ShardFrameCollection(10, bucketSize: 128, supportBufferExpansion: true, bufferExpansionFactor: 1.1f);

            for (long i = 0; i < 10000; i++) {
                shard.Add("Hello" + i);
                Encoding.UTF8.GetString(shard[i].TryToSpan()).Should().Be("Hello" + i);
            }
        }

        [Fact(Skip = "Benchmark only")]
        public void StringFramesAboveIntMaxValue() {
            var shard = new ShardFrameCollection(int.MaxValue / 10, bucketSize: 1000, supportBufferExpansion: true, bufferExpansionFactor: 1.01f);
            _testOutputHelper.WriteLine("Started");

            //append
            var sw = Stopwatch.StartNew();
            for (long i = 0; i < int.MaxValue * 1.05; i++) {
                shard.Add((i % 10).ToString());
                if (i % 10_000_001 == 0)
                    _testOutputHelper.WriteLine($"Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns; progress: {i / (int.MaxValue * 1.05d) * 100:0.00}%");
            }

            _testOutputHelper.WriteLine(shard.Count.ToString());

            //verify
            for (long i = 0; i < int.MaxValue * 1.05; i += 100001) {
                Encoding.UTF8.GetString(shard[i].TryToSpan()).Should().Be((i % 10).ToString());
                _testOutputHelper.WriteLine(i.ToString());
            }

            /*shard.Iterate(0, shard.Count, (index, frame) => {
                Encoding.UTF8.GetString(frame.TryToSpan()).Should().Be((index % 10).ToString());
            });*/
        }

        [Fact(Skip = "Benchmark only")]
        public void StringFrameWriteBenchmark() {
            const int bucketSize = 1000;
            const int initialSize = 10000;
            const float bufferExpansionFactor = 1.1f;

            var shard = new ShardFrameCollection(initialSize, bucketSize: bucketSize, supportBufferExpansion: true, bufferExpansionFactor: bufferExpansionFactor);
            _testOutputHelper.WriteLine("Started");

            //append
            var sw = Stopwatch.StartNew();
            var str = 1.ToString().AsSpan();
            var len = int.MaxValue * 1.05;
            for (long i = 0; i < len; i++) {
                shard.Add(str);
                if (i % 10_000_001 == 0)
                    _testOutputHelper.WriteLine($"Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns; progress: {i / (int.MaxValue * 1.05d) * 100:0.00}%");
            }
        }

        [Fact(Skip = "Benchmark only")]
        public void StringFrameWriteBenchmarkOptimization() {
            const int bucketSize = 1000;
            const int initialSize = 10000;
            const float bufferExpansionFactor = 1.1f;

            var shard = new ShardFrameCollection(initialSize, bucketSize: bucketSize, supportBufferExpansion: true, bufferExpansionFactor: bufferExpansionFactor);
            _testOutputHelper.WriteLine("Started");

            //append
            var sw = Stopwatch.StartNew();
            var str = 1.ToString().AsSpan();
            var len = int.MaxValue * 1.05;
            for (long i = 0; i < len; i++) {
                shard.Add(str);
                if (i % 10_000_001 == 0)
                    _testOutputHelper.WriteLine($"Added {i} items after {sw.ElapsedMilliseconds}ms - perf: {i / sw.Elapsed.TotalSeconds:0} items/s; per item {(sw.Elapsed.TotalMilliseconds / i) * 1000 * 1000:0.000}ns; progress: {i / (int.MaxValue * 1.05d) * 100:0.00}%");
            }
        }

        [Fact]
        public void ShardAboveIntMaxValue() {
            var shard = new ShardFrameCollection((long) (int.MaxValue * 1.05), bucketSize: 128_000, true, 1.01f);
            long i = 0;
            while (shard.BytesLength < int.MaxValue * 1.01) {
                shard.Add(Payload(size: 500, value: (byte) (++i)));
            }

            StrongBox<int> steps = new StrongBox<int>(0);
            shard.Iterate(0, shard.Count, (index, frame) => {
                steps.Value++;
                if (index % 10000 != 0)
                    return; //test every 1000 items
                var pl = Payload(size: 500, value: unchecked((byte) (index + 1)));
                shard[index].ToArray().Should().BeEquivalentTo(pl);
                frame.ToArray().Should().BeEquivalentTo(pl);
                _testOutputHelper.WriteLine(steps.Value.ToString());
            });

            steps.Value.Should().Be((int) shard.Count);
        }

        [Fact(Skip = "Benchmark only")]
        public void ShardCountAboveIntMaxValue() {
            var shard = new ShardFrameCollection((long) (int.MaxValue * 1.05), bucketSize: 128_000, true, 1.01f);
            long i = 0;
            while (shard.Count < int.MaxValue * 1.03) {
                shard.Add(Payload(size: 1, value: (byte) (++i)));
            }

            StrongBox<long> steps = new StrongBox<long>(0);
            shard.Iterate(0, shard.Count, (index, frame) => {
                steps.Value++;
                if (index % 100000 != 0)
                    return; //test every 1000 items
                var pl = Payload(size: 1, value: unchecked((byte) (index + 1)));
                shard[index].ToArray().Should().BeEquivalentTo(pl);
                frame.ToArray().Should().BeEquivalentTo(pl);
                _testOutputHelper.WriteLine(steps.Value.ToString());
            });

            steps.Value.Should().Be(shard.Count);
            _testOutputHelper.WriteLine(shard.Count.ToString());
        }


        [DebuggerStepThrough]
        private byte[] Payload(int size, byte value) {
            var b = new byte[size];
            b.AsSpan().Fill(value);
            return b;
        }
    }
}