using FluentAssertions;
using Nucs.Collections;
using Nucs.Threading;
using Xunit;

namespace Nucs.Essentials.UnitTests;

public class AsyncRoundRobinProducerConsumerTests {
    [Fact]
    public void Basic_LateAdd() {
        var rr = new AsyncRoundRobinProducerConsumer<object>();
        var con1 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con2 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con3 = new AsyncSingleProducerSingleConsumerQueue<object>();
        rr.AddConsumer(con1);
        rr.AddConsumer(con2);

        rr.Enqueue(new object());
        con1.Count.Should().Be(0); //first iteration always skips 1
        con2.Count.Should().Be(1);

        rr.Enqueue(new object());
        con1.Count.Should().Be(1);
        con2.Count.Should().Be(1);

        rr.AddConsumer(con3);
        rr.Enqueue(new object());
        con1.Count.Should().Be(1);
        con2.Count.Should().Be(1);
        con3.Count.Should().Be(1);
    }

    [Fact]
    public void MassEnqueue() {
        var rr = new AsyncRoundRobinProducerConsumer<object>();
        var con1 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con2 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con3 = new AsyncSingleProducerSingleConsumerQueue<object>();
        rr.AddConsumer(con1);
        rr.AddConsumer(con2);
        rr.AddConsumer(con3);

        for (int i = 0; i < 999; i++) {
            rr.Enqueue(new object());
        }

        con1.Count.Should().Be(333);
        con2.Count.Should().Be(333);
        con2.Count.Should().Be(333);
    }

    [Fact]
    public void MassEnqueue_Async() {
        var rr = new AsyncRoundRobinProducerConsumer<object>();
        var con1 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con2 = new AsyncSingleProducerSingleConsumerQueue<object>();
        var con3 = new AsyncSingleProducerSingleConsumerQueue<object>();
        rr.AddConsumer(con1);
        rr.AddConsumer(con2);
        rr.AddConsumer(con3);

        ConcurrentThreadGun.Run(9, number => {
            for (int i = 0; i < 111; i++) {
                rr.Enqueue(new object());
            }
        });

        con1.Count.Should().Be(333);
        con2.Count.Should().Be(333);
        con2.Count.Should().Be(333);
    }
}