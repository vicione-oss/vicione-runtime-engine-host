using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemetry = ViciOne.ManagedEngine.Runtime.EngineChainTelemetry;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class EngineChain(ILogger<EngineChain> logger) : IEngineChain, IDisposable
{
    private readonly ConcurrentDictionary<string, ChainLink> _chainLinks = new();
    private readonly SemaphoreSlim _timerSemaphore = new(1, 1);
    private ITimer? _timer;
    private long _isRunning;

    public event Func<EngineChainCycleReport, Task>? CycleReport;
    public event Func<EngineChainCrashReport, Task>? CrashReport;

    // for tests
    internal EngineChain(ITimer timer, ILogger<EngineChain> logger) : this(logger) => _timer = timer;

    public async Task StopAsync()
    {
        await ResetTimerAsync().ConfigureAwait(false);
        await WaitForOngoingWorkAsync().ConfigureAwait(false);
    }

    private async Task WaitForOngoingWorkAsync()
    {
        while (Interlocked.Read(ref _isRunning) == 1)
            await Task.Delay(1).ConfigureAwait(false);
    }

    private async Task ResetTimerAsync()
    {
        using (await _timerSemaphore.LockAsync())
        {
            if (_timer is not null)
            {
                await _timer.StopAsync().ConfigureAwait(false);
                (_timer as IDisposable)?.Dispose();
                _timer = null;
            }
        }
    }

    public async Task SetUpCycleTimeAsync(TimeSpan cycleTime)
    {
        using (await _timerSemaphore.LockAsync())
        {
            if (_timer is not null)
            {
                if (cycleTime != _timer.Interval)
                    throw new InvalidOperationException($"Cycle time {cycleTime.TotalMilliseconds}ms does not match to chain time {_timer.Interval.TotalMilliseconds}ms.");
            }
            else
            {
                if (OperatingSystem.IsLinux())
                    _timer = new UnixTimer<EngineChainExecutionResult>(cycleTime, ProcessChainLinks, ReportChainResults, logger);
                else
                    _timer = new DefaultTimer<EngineChainExecutionResult>(cycleTime, ProcessChainLinks, ReportChainResults);
                _timer.Start();
            }
        }
    }

    public void AddChainLink(string id, Func<ulong> chainLink, int index)
    {
        ChainLink link = new(chainLink, index);
        if (_chainLinks.Any(cl => cl.Value.Index == index))
            throw new InvalidOperationException($"There is already a chain link with index '{index}'.");
        if (!_chainLinks.TryAdd(id, link))
            throw new InvalidOperationException($"There is already a chain link with id '{id}'.");
        logger.AddedChainLink(id, index);
    }

    public async Task RemoveChainLinkAsync(string id)
    {
        _chainLinks.TryRemove(id, out _);
        logger.RemovedChainLink(id);

        if (_chainLinks.IsEmpty)
            await ResetTimerAsync().ConfigureAwait(false);
    }

    public void EnableChainLink(string id)
    {
        if (_chainLinks.TryGetValue(id, out var chainLink))
            chainLink.Enabled = true;
        else
            throw new InvalidOperationException($"Cannot update enabled state of chain link '{id}' to enabled.");
        logger.EnabledChainLink(id);
    }

    public void DisableChainLink(string id)
    {
        if (_chainLinks.TryGetValue(id, out var chainLink))
            chainLink.Enabled = false;
        else
            throw new InvalidOperationException($"Cannot update enabled state of chain link '{id}' to disabled.");
        logger.DisabledChainLink(id);
    }

    internal EngineChainExecutionResult? ProcessChainLinks(CancellationToken cancellationToken = default)
    {
        var chainLinks = _chainLinks.Where(l => l.Value.Enabled).OrderBy(l => l.Value.Index).ToList();
        if (chainLinks.Count == 0)
            return default;

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            return default;

        EngineChainExecutionResult result = new();

        using (var cycleActivity = Telemetry.StartCycleActivity(chainLinks.Count))
        {
            var chainLinkWatch = new Stopwatch();
            foreach (var (id, chainLink) in chainLinks)
            {
                // the previously determined chain links can be disabled during execution
                if (!chainLink.Enabled || cancellationToken.IsCancellationRequested)
                    continue;

                using var linkActivity = Telemetry.StartChainLinkActivity(id, chainLink.Index);

                ulong? cycle = null;
                chainLinkWatch.Restart();

                try
                {
                    cycle = chainLink.Action();
                    linkActivity?.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    logger.ChainLinkExecutionFailure(chainLink.Index, ex);
                    result.CrashedChainLinks.Add(id);

                    linkActivity?.SetStatus(ActivityStatusCode.Error);
                    Telemetry.IncreaseChainLinkCrashCount(id);
                }

                chainLinkWatch.Stop();
                result.ChainLinkDurations.Add(id, new(cycle, chainLinkWatch.Elapsed));
                Telemetry.RecordChainLinkDuration(id, chainLinkWatch.Elapsed.TotalMilliseconds);
            }
        }

        Interlocked.Exchange(ref _isRunning, 0);

        return result;
    }

    internal async Task ReportChainResults(TimerTick<EngineChainExecutionResult> tick)
    {
        try
        {
            Telemetry.RecordCycleDuration(tick.Duration.TotalMilliseconds);

            if (_timer is not null)
            {
                if (tick.Duration > _timer.Interval)
                {
                    var skippedCycles = (int)(tick.Duration / _timer.Interval);
                    logger.CycleTimeExceeded(tick.Duration.TotalMilliseconds, skippedCycles, FormatChainLinkDurations(tick.Output.ChainLinkDurations));
                    Telemetry.IncreaseCycleSkippedCount(skippedCycles);
                }
            }

            if (tick.Output.CrashedChainLinks.Count != 0)
                await CrashReport.InvokeAsync(new(tick.Output.CrashedChainLinks)).ConfigureAwait(false);

            if (tick.Output.ChainLinkDurations.Count != 0)
                await CycleReport.InvokeAsync(new(tick.Duration, tick.Output.ChainLinkDurations)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.ReportChainResultsFailed(ex);
        }
    }

    private static string FormatChainLinkDurations(Dictionary<string, ChainLinkReport> chainLinkDurations)
    {
        StringBuilder builder = new();
        builder.AppendJoin(Environment.NewLine, chainLinkDurations
            .Select(l => $"{l.Key}: {l.Value.Duration.TotalMilliseconds.ToString("0.00", CultureInfo.InvariantCulture)}ms (cycle {l.Value.Cycle})"));

        if (builder.Length > 0)
            builder.Insert(0, Environment.NewLine);
        return builder.ToString();
    }

    public void Dispose()
    {
        (_timer as IDisposable)?.Dispose();
        _timerSemaphore.Dispose();
    }

    private sealed record ChainLink(Func<ulong> Action, int Index)
    {
        private long _enabled;
        internal bool Enabled
        {
            get => Interlocked.Read(ref _enabled) == 1;
            set => Interlocked.Exchange(ref _enabled, value ? 1 : 0);
        }
    }
}
