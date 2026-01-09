using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine;

public static class SemaphoreSlimExtensions
{
    internal static IDisposable Lock(this SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new Disposable(() => semaphore.Release());
    }

    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        return new Disposable(() => semaphore.Release());
    }
}
