using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class DefaultTimer<T> : ITimer, IDisposable
{
    private readonly Func<CancellationToken, T?> _action;
    private readonly Func<TimerTick<T>, Task> _report;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _workerCancellation;
    private Task? _worker;

    internal DefaultTimer(TimeSpan interval, Func<CancellationToken, T?> action, Func<TimerTick<T>, Task> report)
    {
        Interval = interval;
        _action = action;
        _report = report;
    }

    public TimeSpan Interval { get; }

    public void Start()
    {
        if (_timer is null)
        {
            _timer = new(Interval);
            _workerCancellation = new();
            _worker = CallPeriodicallyAsync(_timer, _action, _report, _workerCancellation.Token);
        }
    }

    private static async Task CallPeriodicallyAsync(PeriodicTimer timer, Func<CancellationToken, T?> action, Func<TimerTick<T>, Task> report, CancellationToken cancellationToken)
    {
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            var watch = Stopwatch.StartNew();
            var output = action(cancellationToken);
            watch.Stop();
            if (output is not null)
                _ = Task.Run(() => report(new TimerTick<T>(watch.Elapsed, output)), cancellationToken);
        }
    }

    public async Task StopAsync()
    {
        if (_timer is not null)
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

            _timer.Dispose();
            _workerCancellation?.Dispose();

            _timer = null;
            _workerCancellation = null;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _workerCancellation?.Dispose();

        _timer = null;
        _workerCancellation = null;
    }
}
