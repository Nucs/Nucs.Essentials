using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Nucs.Collections.Structs;
using Xunit;

namespace Nucs.Essentials.UnitTests {
    public unsafe class ShardFrameCollectionTests {
        [Fact]
        public void ShardAddAndIndexer() {
            var shard = new ShardFrameCollection(new byte[128], 100);
            shard.Add(Payload(size: 3, value: 1));
            shard.Add(Payload(size: 3, value: 2));

            var item1 = shard[0];
            var item2 = shard[1];

            item1.ToArray().Should().BeEquivalentTo(Payload(size: 3, value: 1));
            item2.ToArray().Should().BeEquivalentTo(Payload(size: 3, value: 2));
        }

        [Fact]
        public void ShardAddAndIndexerFor20000Items() {
            var shard = new ShardFrameCollection(new byte[140_000], bucketSize: 100);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardAddAndIndexerWith1BucketSize() {
            var shard = new ShardFrameCollection(new byte[140_000], bucketSize: 1);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }

        [Fact]
        public void ShardAddAndResize() {
            var shard = new ShardFrameCollection(new byte[10], bucketSize: 1, true, 1.1f);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }
        
        [Fact]
        public void ShardAddAndResizeFraction() {
            var shard = new ShardFrameCollection(new byte[10], bucketSize: 1, true, 1.01f);
            for (int i = 0; i < 20000; i++) {
                shard.Add(Payload(size: 3, value: (byte) (i + 1)));
            }

            for (int i = 0; i < 20000; i++) {
                shard[i].ToArray().Should().BeEquivalentTo(Payload(size: 3, value: unchecked((byte) (i + 1))));
            }
        }
        
        
        [Fact]
        public void ShardAddAndIndexerFor20000ItemsIterate() {
            var shard = new ShardFrameCollection(new byte[140_000], bucketSize: 100);
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


        [DebuggerStepThrough]
        private byte[] Payload(int size, byte value) {
            var b = new byte[size];
            b.AsSpan().Fill(value);
            return b;
        }
    }
}