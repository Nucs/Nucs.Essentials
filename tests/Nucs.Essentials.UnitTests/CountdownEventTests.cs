using System;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Threading;
using DotNext.Threading.Tasks;
using FluentAssertions;
using Nucs.Threading;
using Xunit;
using AsyncCountdownEvent = Nucs.Threading.AsyncCountdownEvent;

namespace Nucs.Essentials.UnitTests;

public class CountdownEventTests {
    [Fact]
    public void Case1() {
        var cd = new AsyncCountdownEvent();
        cd.Add(2);
        var waiter = cd.WaitAsync();
        waiter.IsCompleted.Should().BeFalse();
        cd.Signal(2);
        waiter.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void Case2() {
        var cd = new AsyncCountdownEvent();
        cd.Add();
        cd.Add();
        var waiter = cd.WaitAsync();
        waiter.IsCompleted.Should().BeFalse();
        cd.Signal();
        waiter.IsCompletedSuccessfully.Should().BeFalse();
        cd.Signal();
        waiter.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task Case3_RegularSignal() {
        var cd = new AsyncCountdownEvent();
        cd.Add(10);
        var gun = ConcurrentThreadGun.RunAsync(10, _ => cd.Signal());
        await gun;
        await cd.WaitAsync();
    }

    [Fact]
    public async Task Case4_CompareExchangeSignal() {
        var cd = new AsyncCountdownEvent();
        cd.Add(10);
        var gun = ConcurrentThreadGun.RunAsync(10, _ => cd.Signal(1));
        await gun;
        await cd.WaitAsync();
    }

    [Fact]
    public async Task Case5_BothSignals() {
        var cd = new AsyncCountdownEvent();
        cd.Add(5 * 2 + 5);
        var gun = ConcurrentThreadGun.RunDiverseAsync(10, _ => cd.Signal(2), _ => cd.Signal(1));
        await gun;
        await cd.WaitAsync();
    }
}