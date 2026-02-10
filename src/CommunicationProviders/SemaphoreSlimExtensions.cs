using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine;

/// <summary>
/// Provides extension methods for <see cref="SemaphoreSlim"/>.
/// </summary>
public static class SemaphoreSlimExtensions
{
    internal static IDisposable Lock(this SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new Disposable(() => semaphore.Release());
    }

    /// <summary>
    /// Asynchronously acquires a lock on the semaphore and returns a disposable that releases it.
    /// </summary>
    /// <param name="semaphore">The semaphore to lock.</param>
    /// <returns>A disposable that releases the semaphore when disposed.</returns>
    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync().ConfigureAwait(false);
        return new Disposable(() => semaphore.Release());
    }
}
