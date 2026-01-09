using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class TransactionContext : IDisposable
{
    internal const int Timeout = 10_000;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    public void Register(string id)
        => _semaphores.TryAdd(id, new(1));

    public void Unregister(string id)
    {
        if (_semaphores.TryRemove(id, out var semaphore))
            semaphore.Dispose();
    }

    public async Task<IDisposable> WaitAsync(string id, int timeout)
    {
        if (_semaphores.TryGetValue(id, out var semaphore))
        {
            if (!await semaphore.WaitAsync(timeout).ConfigureAwait(false))
                throw new OperationCanceledException("Transaction wait timeout expired. Try again later.");
            return new Disposable(() => semaphore.Release());
        }

        return Disposable.Empty;
    }

    public void Dispose()
    {
        foreach (var semaphore in _semaphores.Values)
            semaphore.Dispose();
    }
}
