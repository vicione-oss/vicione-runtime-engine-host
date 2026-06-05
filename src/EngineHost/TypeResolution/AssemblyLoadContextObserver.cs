using System;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.TypeResolution;

internal static class AssemblyLoadContextObserver
{
    private const int ContextAliveInformationDelay = 5_000;
    private const int ContextAliveWarningDelay = 30_000;
    private const int MaxObservationDelay = 60_000;
    private const int DefaultPollInterval = 5_000;

    internal static Task Observe(AssemblyLoadContext context, Action logEarly, Action logLate, Action logUnloaded,
        Action? onUnloaded = null, Action? logLeaked = null,
        int logEarlyDelay = ContextAliveInformationDelay, int logLateDelay = ContextAliveWarningDelay,
        int maxWaitDelay = MaxObservationDelay, int pollInterval = DefaultPollInterval,
        TimeProvider? timeProvider = null, Func<bool>? isAlive = null, Func<Task>? tryEnsureUnload = null,
        Func<Task>? tryEnsureUnloadAggressive = null)
    {
        WeakReference contextReference = new(context);

        return ObserveCore(
            isAlive ?? (() => contextReference.IsAlive),
            tryEnsureUnload ?? (() => TryEnsureUnload(contextReference)),
            tryEnsureUnloadAggressive ?? (() => TryEnsureUnloadAggressive(contextReference)),
            timeProvider ?? TimeProvider.System,
            logEarly, logLate, logUnloaded, onUnloaded, logLeaked,
            logEarlyDelay, logLateDelay, maxWaitDelay, pollInterval);
    }

    private static async Task ObserveCore(Func<bool> isAlive, Func<Task> tryEnsureUnload,
        Func<Task> tryEnsureUnloadAggressive, TimeProvider timeProvider,
        Action logEarly, Action logLate, Action logUnloaded, Action? onUnloaded, Action? logLeaked,
        int logEarlyDelay, int logLateDelay, int maxWaitDelay, int pollInterval)
    {
        await tryEnsureUnload().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromMilliseconds(logEarlyDelay), timeProvider).ConfigureAwait(false);

        if (!isAlive())
        {
            logUnloaded();
            onUnloaded?.Invoke();
            return;
        }

        logEarly();
        await tryEnsureUnloadAggressive().ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromMilliseconds(logLateDelay), timeProvider).ConfigureAwait(false);

        if (!isAlive())
        {
            logUnloaded();
            onUnloaded?.Invoke();
            return;
        }

        logLate();

        var elapsed = logEarlyDelay + logLateDelay;
        while (elapsed < maxWaitDelay)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(pollInterval), timeProvider).ConfigureAwait(false);
            elapsed += pollInterval;

            if (!isAlive())
            {
                logUnloaded();
                onUnloaded?.Invoke();
                return;
            }
        }

        logLeaked?.Invoke();
    }

    private static async Task TryEnsureUnload(WeakReference reference)
    {
        for (var i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(50, default).ConfigureAwait(false);
        }
    }

    private static async Task TryEnsureUnloadAggressive(WeakReference reference)
    {
        for (var i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive);
            GC.WaitForPendingFinalizers();
            await Task.Delay(50, default).ConfigureAwait(false);
        }
    }
}
