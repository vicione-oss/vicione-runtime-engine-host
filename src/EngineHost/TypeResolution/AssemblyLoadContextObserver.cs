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

    internal static void Observe(AssemblyLoadContext context, Action logEarly, Action logLate, Action logUnloaded,
        Action? onUnloaded = null, Action? logLeaked = null,
        int logEarlyDelay = ContextAliveInformationDelay, int logLateDelay = ContextAliveWarningDelay,
        int maxWaitDelay = MaxObservationDelay, int pollInterval = DefaultPollInterval)
    {
        WeakReference contextReference = new(context);
        Task.Run(async () =>
        {
            await Task.Delay(logEarlyDelay);

            if (!contextReference.IsAlive)
            {
                logUnloaded();
                onUnloaded?.Invoke();
                return;
            }

            logEarly();

            await Task.Delay(logLateDelay);

            if (!contextReference.IsAlive)
            {
                logUnloaded();
                onUnloaded?.Invoke();
                return;
            }

            logLate();

            var elapsed = logEarlyDelay + logLateDelay;
            while (elapsed < maxWaitDelay)
            {
                await Task.Delay(pollInterval);
                elapsed += pollInterval;

                if (!contextReference.IsAlive)
                {
                    logUnloaded();
                    onUnloaded?.Invoke();
                    return;
                }
            }

            logLeaked?.Invoke();
        });
    }
}
