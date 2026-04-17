using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ViciOne.ManagedEngine.Runtime;

public class EngineChain_ProcessChainLinks
{
    [Fact]
    public void Does_not_execute_disabled_chain_links()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var calls = 0u;

        engineChain.AddChainLink("1", () => calls++, 0);
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Should().Be(0);
    }

    [Fact]
    public void Executes_enabled_chain_links()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var calls = 0u;

        engineChain.AddChainLink("1", () => calls++, 0);
        engineChain.EnableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Should().Be(1);
    }

    [Fact]
    public void Executes_chain_links_in_order()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        List<uint> calls = [];

        engineChain.AddChainLink("1", () => { calls.Add(1); return 0; }, 10);
        engineChain.AddChainLink("2", () => { calls.Add(2); return 1; }, 1);
        engineChain.EnableChainLink("1");
        engineChain.EnableChainLink("2");

        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Should().HaveCount(2).And.ContainInOrder(2, 1);
    }

    [Fact]
    public void Can_handle_enabled_change()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var calls = 0u;

        engineChain.AddChainLink("1", () => calls++, 0);

        engineChain.EnableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        calls.Should().Be(1);

        engineChain.DisableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        calls.Should().Be(1);

        engineChain.EnableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        calls.Should().Be(2);
    }

    [Fact]
    public void Skips_disabled_chain_link_while_executing()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        List<uint> calls = [];
        engineChain.AddChainLink("1", () => { engineChain.DisableChainLink("2"); calls.Add(1); return 0; }, 0);
        engineChain.AddChainLink("2", () => { calls.Add(2); return 0; }, 1);
        engineChain.EnableChainLink("1");
        engineChain.EnableChainLink("2");

        _ = engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public async Task Does_not_execute_removed_chain_link()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var calls = 0u;

        engineChain.AddChainLink("1", () => calls++, 0);
        engineChain.EnableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        calls.Should().Be(1);

        await engineChain.RemoveChainLinkAsync("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        calls.Should().Be(1);
    }

    [Fact]
    public void Can_be_stopped()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        List<uint> calls = [];
        using CancellationTokenSource cts = new();
        engineChain.AddChainLink("1", () => { cts.CancelAsync(); calls.Add(1); return 0; }, 0);
        engineChain.AddChainLink("2", () => { calls.Add(2); return 0; }, 1);
        engineChain.EnableChainLink("1");
        engineChain.EnableChainLink("2");

        _ = engineChain.ProcessChainLinks(cts.Token);

        calls.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public async Task Does_not_skip_empty_cycle()
    {
        TestLogger<EngineChain> logger = new(LogLevel.Warning);
        using EngineChain engineChain = new(Substitute.For<ITimer>(), logger);
        var calls = 0u;
        using ManualResetEventSlim actionStarted = new();
        engineChain.AddChainLink("1", () => { actionStarted.Set(); Thread.Sleep(150.Milliseconds()); return calls++; }, 0);
        engineChain.EnableChainLink("1");

        var cycle1 = Task.Run(() => engineChain.ProcessChainLinks(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        actionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        engineChain.DisableChainLink("1");
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        await cycle1;

        calls.Should().Be(1);
        logger.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Can_handle_violation_of_cycle_time()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var calls = 0u;
        using ManualResetEventSlim actionStarted = new();

        engineChain.AddChainLink("1", () =>
        {
            calls++;
            actionStarted.Set();
            Thread.Sleep(225);
            return 0;
        }, 0);
        engineChain.EnableChainLink("1");

        // First call is being processed
        var firstProcess = Task.Run(() => engineChain.ProcessChainLinks(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        // Wait until the first call is actually running
        actionStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        // Second call returns null because first is still running
        var secondResult = engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        await firstProcess;

        calls.Should().Be(1);
        secondResult.Should().BeNull();
    }

    [Fact]
    public void Internal_processing_works_like_expected()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        Dictionary<string, int> calls = new() { { "1", 0 }, { "2", 0 }, };

        engineChain.AddChainLink("1", () =>
        {
            calls["1"]++;
            Thread.Sleep(10);
            return 0;
        }, 0);
        engineChain.AddChainLink("2", () =>
        {
            calls["2"]++;
            Thread.Sleep(10);
            return 0;
        }, 1);
        engineChain.EnableChainLink("1");
        engineChain.EnableChainLink("2");

        // Simulate 3 cycles
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Values.Should().HaveCount(2).And.AllSatisfy(v => v.Should().Be(3));
    }

    [Fact]
    public void Can_handle_execution_failures()
    {
        var calls = 0;
        TestLogger<EngineChain> logger = new(LogLevel.Warning);
        using EngineChain engineChain = new(Substitute.For<ITimer>(), logger);
        engineChain.AddChainLink("1",
            () =>
            {
                calls++;
                throw new InvalidOperationException("error");
            }, 0);
        engineChain.EnableChainLink("1");

        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);
        engineChain.ProcessChainLinks(TestContext.Current.CancellationToken);

        calls.Should().Be(2);
        logger.EventId.Id.Should().Be(2);
        logger.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("error");
        logger.LogLevel.Should().Be(LogLevel.Error);
        logger.Message.Should().Match("*chain*link*fail*0*");
    }

}

public class EngineChain_StopAsync
{
    [Fact]
    public async Task Waits_until_processing_finishes()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        using AutoResetEvent tick = new(false);
        engineChain.AddChainLink("1", () =>
        {
            tick.Set();
            Thread.Sleep(250);
            return 0;
        }, 0);
        engineChain.EnableChainLink("1");

        var processTask = Task.Run(() => engineChain.ProcessChainLinks(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        tick.WaitOne();
        var stopWatch = Stopwatch.StartNew();
        await engineChain.StopAsync();
        await processTask;
        stopWatch.Stop();

        stopWatch.Elapsed.Should().BeCloseTo(250.Milliseconds(), 100.Milliseconds());
    }

    [Fact]
    public async Task Stops_timer()
    {
        var timer = Substitute.For<ITimer>();
        using EngineChain engineChain = new(timer, NullLogger<EngineChain>.Instance);

        await engineChain.StopAsync();

        await timer.Received(1).StopAsync();
    }

    [Fact]
    public async Task Disposes_timer()
    {
        var timer = Substitute.For<ITimer, IDisposable>();
        using EngineChain engineChain = new(timer, NullLogger<EngineChain>.Instance);

        await engineChain.StopAsync();

        ((IDisposable)timer).Received(1).Dispose();
    }
}

public class EngineChain_ReportChainResults
{
    [Fact]
    public async Task Logs_cycle_time_exceeded()
    {
        var timer = Substitute.For<ITimer>();
        timer.Interval.Returns(1.Milliseconds());
        TestLogger<EngineChain> logger = new(LogLevel.Warning);
        using EngineChain engineChain = new(timer, logger);
        EngineChainExecutionResult tick = new();
        tick.ChainLinkDurations.Add("1", new(0, 10.Milliseconds()));

        await engineChain.ReportChainResults(new TimerTick<EngineChainExecutionResult>(10.Milliseconds(), tick));

        logger.Entries.Should().ContainSingle();
        logger.EventId.Id.Should().Be(3);
        logger.Exception.Should().BeNull();
        logger.LogLevel.Should().Be(LogLevel.Warning);
        logger.Message.Should().MatchEquivalentOf("*cycle*took*10*skip*10*cycles*");
    }

    [Fact]
    public async Task Reports_durations_correctly()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        EngineChainCycleReport? report = null;
        engineChain.CycleReport += r => { report = r; return Task.CompletedTask; };
        EngineChainExecutionResult tick = new();
        tick.ChainLinkDurations.Add("1", new(0, 700.Milliseconds()));
        tick.ChainLinkDurations.Add("2", new(1, 300.Milliseconds()));

        await engineChain.ReportChainResults(new TimerTick<EngineChainExecutionResult>(1.Seconds(), tick));

        report.Should().NotBeNull();
        report!.Duration.Should().Be(1.Seconds());
        report.ChainLinkReports["1"].Duration.Should().Be(700.Milliseconds());
        report.ChainLinkReports["2"].Duration.Should().Be(300.Milliseconds());
        report.ChainLinkReports["1"].Cycle.Should().Be(0);
        report.ChainLinkReports["2"].Cycle.Should().Be(1);
    }

    [Fact]
    public async Task Reports_nothing_without_enabled_chain_links()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        var call = false;
        engineChain.CycleReport += _ => { call = true; return Task.CompletedTask; };
        EngineChainExecutionResult tick = new();

        await engineChain.ReportChainResults(new TimerTick<EngineChainExecutionResult>(TimeSpan.Zero, tick));

        call.Should().BeFalse();
    }

    [Fact]
    public async Task Reports_crashed_chain_links()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        EngineChainCrashReport? report = null;
        engineChain.CrashReport += r => { report = r; return Task.CompletedTask; };
        EngineChainExecutionResult tick = new();
        tick.CrashedChainLinks.Add("1");

        await engineChain.ReportChainResults(new TimerTick<EngineChainExecutionResult>(TimeSpan.Zero, tick));

        report.Should().NotBeNull();
        report!.CrashedChainLinks.Should().ContainSingle("1");
    }
}

public class EngineChain_AddChainLink
{
    [Fact]
    public void Throws_if_chain_link_already_exists()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        engineChain.AddChainLink("1", () => 0, 1);

        var act = FluentActions.Invoking(() => engineChain.AddChainLink("1", () => 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*already*link*1*");
    }

    [Fact]
    public void Throws_if_chain_link_with_same_index_already_exists()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);
        engineChain.AddChainLink("1", () => 0, 0);

        var act = FluentActions.Invoking(() => engineChain.AddChainLink("1", () => 0, 0));

        act.Should().Throw<InvalidOperationException>().WithMessage("*already*link*0*");
    }
}

public class EngineChain_RemoveChainLinkAsync
{
    [Fact]
    public async Task Resets_chain_after_removing_the_last_chain_link()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);

        await engineChain.RemoveChainLinkAsync("1");

        // After removing all chain links, the timer should be reset
        // and we should be able to set up a new cycle time
        var act = () => engineChain.SetUpCycleTimeAsync(10.Milliseconds());

        await act.Should().NotThrowAsync();
    }
}

public class EngineChain_EnableChainLink
{
    [Fact]
    public void Throws_if_chain_link_does_not_exist()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);

        var act = FluentActions.Invoking(() => engineChain.EnableChainLink("1"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*not*update*state*1*enable*");
    }
}

public class EngineChain_DisableChainLink
{
    [Fact]
    public void Throws_if_chain_link_does_not_exist()
    {
        using EngineChain engineChain = new(Substitute.For<ITimer>(), NullLogger<EngineChain>.Instance);

        var act = FluentActions.Invoking(() => engineChain.DisableChainLink("1"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*not*update*state*1*disable*");
    }
}

public class EngineChain_SetUpCycleTime
{
    [Fact]
    public async Task Throws_if_cycle_time_does_not_match_to_chain()
    {
        var timer = Substitute.For<ITimer>();
        timer.Interval.Returns(100.Milliseconds());
        using EngineChain engineChain = new(timer, NullLogger<EngineChain>.Instance);

        var act = FluentActions.Invoking(() => engineChain.SetUpCycleTimeAsync(10.Milliseconds()));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*time*10*not*match*chain*100*");
    }
}
