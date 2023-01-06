using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nucs.Collections;
using Nucs.Reflection;
using Nucs.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Nucs.Essentials.UnitTests;

public class ManyProducerManyConsumerStackTests {
    private readonly ITestOutputHelper Console;

    public ManyProducerManyConsumerStackTests(ITestOutputHelper console) {
        Console = console;
    }

    [Fact]
    public void SequentialEnqDeq() {
        var queue = new ManyProducerManyConsumerStack<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.TryDequeue(out var i).Should().BeTrue();
        i.Should().Be(2);

        queue.TryDequeue(out i).Should().BeTrue();
        i.Should().Be(1);
    }

    [Fact]
    public void SequentialEnqDeq_Fail() {
        var queue = new ManyProducerManyConsumerStack<int>();

        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.TryDequeue(out var i).Should().BeTrue();
        i.Should().Be(2);

        queue.TryDequeue(out i).Should().BeTrue();
        i.Should().Be(1);

        queue.TryDequeue(out i).Should().BeFalse();
        i.Should().Be(default(int));
    }

    [Fact]
    public void NewAndEmpty() {
        var queue = new ManyProducerManyConsumerStack<int>();

        queue.TryDequeue(out var i).Should().BeFalse();
        i.Should().Be(default(int));
    }

    [Fact(Timeout = 1000)]
    public async Task Concurrent_Deq() {
        var queue = new ManyProducerManyConsumerStack<int>();
        var l = new ConcurrentList<int>();
        const int threads = 16;
        for (int i = 1; i <= threads; i++) {
            queue.Enqueue(i);
            l.Add(i);
        }

        await ConcurrentThreadGun.RunAsync(threads, number => {
            queue.TryDequeue(out var i);
            Console.WriteLine(i.ToString());
            l.Remove(i);
        });

        l.Should().BeEmpty();
    }

    [Fact(Timeout = 1000)]
    public async Task Concurrent_EnqRangeDeq() {
        var queue = new ManyProducerManyConsumerStack<int>();
        var l = new ConcurrentList<int>();
        const int threads = 16;
        for (int i = 1; i <= threads; i++) {
            l.Add(i);
        }
        
        queue.EnqueueRange(Enumerable.Range(1, 16));

        await ConcurrentThreadGun.RunAsync(threads, number => {
            queue.TryDequeue(out var i);
            Console.WriteLine(i.ToString());
            l.Remove(i);
        });

        l.Should().BeEmpty();
    }


    [Fact(Timeout = 1000)]
    public async Task Concurrent_EnqThenDeq() {
        var queue = new ManyProducerManyConsumerStack<int>();
        var lRem = new ConcurrentList<int>();
        var lAdd = new ConcurrentList<int>();
        for (int j = 0; j < 16; j++) {
            lAdd.Add(j + 1);
            lRem.Add(j + 1);
        }

        StrongBox<int> i = 0;
        await ConcurrentThreadGun.RunAsync(16, number => {
            number = Interlocked.Increment(ref i.Value);
            queue.Enqueue(number);
            Console.WriteLine(number.ToString());
            lAdd.Remove(number);
        });

        Console.WriteLine("------");

        await ConcurrentThreadGun.RunAsync(16, number => {
            queue.TryDequeue(out var i);
            Console.WriteLine(i.ToString());
            lRem.Remove(i);
        });

        lAdd.Should().BeEmpty();
        lRem.Should().BeEmpty();
    }

    [Fact(Timeout = 1000)]
    public async Task Concurrent_EnqAndDeq() {
        var queue = new ManyProducerManyConsumerStack<int>();
        var lRem = new ConcurrentList<int>();
        var lAdd = new ConcurrentList<int>();
        for (int j = 0; j < 16; j++) {
            lAdd.Add(j + 1);
            lRem.Add(j + 1);
        }

        StrongBox<int> i = 0;
        await ConcurrentThreadGun.RunDiverseAsync(16 * 2, number => {
            number = Interlocked.Increment(ref i.Value);
            queue.Enqueue(number);
            Console.WriteLine(number.ToString());
            lAdd.Remove(number);
        }, number => {
            int i;
            while (!queue.TryDequeue(out i)) { }
            Console.WriteLine(i.ToString());
            lRem.Remove(i);
        });


        lAdd.Should().BeEmpty();
        lRem.Should().BeEmpty();
    }
}