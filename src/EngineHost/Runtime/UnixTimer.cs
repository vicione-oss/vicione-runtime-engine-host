using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.ManagedEngine.Runtime;

/// <summary>
/// Unix specific timer implementation using timerfd.
/// </summary>
[SupportedOSPlatform(nameof(OSPlatform.Linux))]
internal sealed class UnixTimer<T> : ITimer, IDisposable
{
    private readonly Func<CancellationToken, T?> _action;
    private readonly Func<TimerTick<T>, Task> _report;
    private readonly ILogger _logger;
    private TimerFd? _nativeTimer;
    private CancellationTokenSource? _workerCancellation;
    private Task? _worker;

    /// <summary>
    /// Interval must be between 10 millisecond and 5 seconds.
    /// </summary>
    internal UnixTimer(TimeSpan interval, Func<CancellationToken, T?> action, Func<TimerTick<T>, Task> report, ILogger logger)
    {
        if (interval.TotalMilliseconds < 10)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be at least 10 millisecond.");
        if (interval.TotalSeconds > 5)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be at most 5 seconds.");

        Interval = interval;
        _action = action;
        _report = report;
        _logger = logger;
    }

    public TimeSpan Interval { get; }

    public void Start()
    {
        if (_nativeTimer is null)
        {
            _workerCancellation = new();
            _nativeTimer = new();
            _nativeTimer.SetPeriod(Interval, Interval);

            _worker = Task.Run(() => CallPeriodically(_nativeTimer, _action, _report, _workerCancellation.Token));
        }
    }

    private void CallPeriodically(TimerFd timer, Func<CancellationToken, T?> action, Func<TimerTick<T>, Task> report,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ulong expirations;

            try
            {
                expirations = timer.WaitForNextExpiry(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWaitForNextExpiryFailed(ex);
                break;
            }

            if (expirations == 0)
                break;

            if (cancellationToken.IsCancellationRequested)
                break;

            var watch = Stopwatch.StartNew();
            var output = action(cancellationToken);
            watch.Stop();
            if (output is not null)
                _ = Task.Run(() => report(new TimerTick<T>(watch.Elapsed, output)), CancellationToken.None);
        }
    }

    /// <summary>
    /// It can't abort blocking waits. So Stop has to wait for the next tick. But doesn't execute the callback.
    /// </summary>
    public async Task StopAsync()
    {
        if (_nativeTimer is not null)
        {
            if (_workerCancellation is not null)
                await _workerCancellation.CancelAsync();

            if (_worker is not null)
            {
                try
                {
                    await _worker.ConfigureAwait(false);
                    _worker = null;
                }
                catch (OperationCanceledException) { }
            }

            _workerCancellation?.Dispose();

            _workerCancellation = null;
        }
    }

    public void Dispose()
    {
        _nativeTimer?.Dispose();
        _workerCancellation?.Dispose();

        _nativeTimer = null;
        _workerCancellation = null;
    }
}
