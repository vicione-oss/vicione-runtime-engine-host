using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)])]
public class UnixTimer_Constructor
{
    [Fact]
    public void Stores_interval()
    {
        var interval = TimeSpan.FromMilliseconds(100);

        using UnixTimer<int> timer = new(interval, _ => 0, _ => Task.CompletedTask, NullLogger.Instance);

        timer.Interval.Should().Be(interval);
    }

    [Fact]
    public void Throws_when_interval_below_minimum()
    {
        var act = () => new UnixTimer<int>(TimeSpan.FromMilliseconds(5), _ => 0, _ => Task.CompletedTask, NullLogger.Instance);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("interval");
    }

    [Fact]
    public void Throws_when_interval_above_maximum()
    {
        var act = () => new UnixTimer<int>(TimeSpan.FromSeconds(10), _ => 0, _ => Task.CompletedTask, NullLogger.Instance);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("interval");
    }
}

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)])]
public class UnixTimer_Start
{
    [Fact]
    public void Does_not_call_action_immediately()
    {
        var actionCalled = false;
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => { actionCalled = true; return 0; },
            _ => Task.CompletedTask,
            NullLogger.Instance);

        timer.Start();

        actionCalled.Should().BeFalse();
    }

    [Fact]
    public void Can_be_called_multiple_times()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);
        timer.Start();

        var act = timer.Start;

        act.Should().NotThrow();
    }
}

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)])]
public class UnixTimer_Stop
{
    [Fact]
    public async Task Cancels_worker()
    {
        var callCount = 0;
        using ManualResetEventSlim actionCalled = new();
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            ct =>
            {
                Interlocked.Increment(ref callCount);
                actionCalled.Set();
                return 0;
            },
            _ => Task.CompletedTask,
            NullLogger.Instance);
        timer.Start();
        var reached = actionCalled.Wait(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        await timer.StopAsync();
        var countAfterStop = callCount;
        await Task.Delay(50, TestContext.Current.CancellationToken);

        reached.Should().BeTrue();
        callCount.Should().Be(countAfterStop);
    }

    [Fact]
    public async Task Does_not_throw_without_start()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);

        var act = timer.StopAsync;

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Can_be_called_multiple_times()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);
        timer.Start();
        await timer.StopAsync();

        var act = timer.StopAsync;

        await act.Should().NotThrowAsync();
    }
}

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)])]
public class UnixTimer_Action
{
    [Fact]
    public async Task Calls_action_on_each_tick()
    {
        var callCount = 0;
        using ManualResetEventSlim secondCallReached = new();
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            _ =>
            {
                if (Interlocked.Increment(ref callCount) >= 2)
                    secondCallReached.Set();
                return 0;
            },
            _ => Task.CompletedTask,
            NullLogger.Instance);

        timer.Start();
        var reached = secondCallReached.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        await timer.StopAsync();

        reached.Should().BeTrue();
        callCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Passes_cancellation_token_to_action()
    {
        CancellationToken receivedToken = default;
        using ManualResetEventSlim actionCalled = new();
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            ct =>
            {
                receivedToken = ct;
                actionCalled.Set();
                return 0;
            },
            _ => Task.CompletedTask,
            NullLogger.Instance);

        timer.Start();
        var called = actionCalled.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await timer.StopAsync();

        called.Should().BeTrue();
        receivedToken.CanBeCanceled.Should().BeTrue();
    }
}

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)])]
public class UnixTimer_Report
{
    [Fact]
    public async Task Measures_action_duration()
    {
        TimerTick<int>? reportedTick = null;
        using ManualResetEventSlim reportReceived = new();
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            _ =>
            {
                Thread.Sleep(50);
                return 42;
            },
            tick =>
            {
                reportedTick = tick;
                reportReceived.Set();
                return Task.CompletedTask;
            },
            NullLogger.Instance);

        timer.Start();
        var received = reportReceived.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await timer.StopAsync();

        received.Should().BeTrue();
        reportedTick.Should().NotBeNull();
        reportedTick!.Output.Should().Be(42);
        reportedTick.Duration.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40));
    }

    [Fact]
    public async Task Does_not_report_null_output()
    {
        var reportCalled = false;
        using ManualResetEventSlim actionCalled = new();

        using UnixTimer<string> timer = new(
            TimeSpan.FromMilliseconds(10),
            _ =>
            {
                actionCalled.Set();
                return null;
            },
            _ =>
            {
                reportCalled = true;
                return Task.CompletedTask;
            },
            NullLogger.Instance);

        timer.Start();
        actionCalled.Wait(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await timer.StopAsync();

        reportCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Report_handling_does_not_block_action()
    {
        var actionCallCount = 0;
        using ManualResetEventSlim reportStarted = new();
        using ManualResetEventSlim secondActionCalled = new();
        using UnixTimer<int> timer = new(
            TimeSpan.FromMilliseconds(10),
            _ =>
            {
                if (Interlocked.Increment(ref actionCallCount) >= 2)
                    secondActionCalled.Set();
                return 42;
            },
            async _ =>
            {
                reportStarted.Set();
                await Task.Delay(300);
            },
            NullLogger.Instance);

        timer.Start();
        reportStarted.Wait(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        // Second action call should not be blocked by slow report handler
        var secondActionReached = secondActionCalled.Wait(TimeSpan.FromMilliseconds(100), TestContext.Current.CancellationToken);
        await timer.StopAsync();

        secondActionReached.Should().BeTrue();
        actionCallCount.Should().BeGreaterThanOrEqualTo(2);
    }
}

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[OSTest(Supported = [nameof(OSPlatform.Linux)], Unsupported = [nameof(OSPlatform.Windows)])]
public class UnixTimer_Dispose
{
    [Fact]
    public void Cleans_up_resources()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);
        timer.Start();

        var act = timer.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public void Does_not_throw_without_start()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);

        var act = timer.Dispose;

        act.Should().NotThrow();
    }

    [Fact]
    public void Can_be_called_multiple_times()
    {
        using UnixTimer<int> timer = new(
            TimeSpan.FromSeconds(5),
            _ => 0,
            _ => Task.CompletedTask,
            NullLogger.Instance);
        timer.Dispose();

        var act = timer.Dispose;

        act.Should().NotThrow();
    }
}
