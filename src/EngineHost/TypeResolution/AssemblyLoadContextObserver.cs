using System;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.TypeResolution;

internal static class AssemblyLoadContextObserver
{
    private const int ContextAliveInformationDelay = 5_000;
    private const int ContextAliveWarningDelay = 30_000;

    internal static void Observe(AssemblyLoadContext context, Action logEarly, Action logLate, Action logUnloaded, int logEarlyDelay = ContextAliveInformationDelay, int logLateDelay = ContextAliveWarningDelay)
    {
        WeakReference contextReference = new(context);
        Task.Run(async () =>
        {
            await Task.Delay(logEarlyDelay);

            if (contextReference.IsAlive)
            {
                logEarly();
            }
            else
            {
                logUnloaded();
                return;
            }

            await Task.Delay(logLateDelay);

            if (contextReference.IsAlive)
                logLate();
            else
                logUnloaded();
        });
    }
}
