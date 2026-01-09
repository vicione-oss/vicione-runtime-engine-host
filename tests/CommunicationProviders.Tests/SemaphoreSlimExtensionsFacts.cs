using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine;

public sealed class SemaphoreSlimExtension_Lock
{
    [Fact]
    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method")]
    public async Task Locks_execution_Async()
    {
        using SemaphoreSlim mutex = new(1, 1);
        List<int> results = [];
        var task = Task.CompletedTask;

        using (mutex.Lock())
        {
            task = Task.Run(() =>
            {
                using (mutex.Lock())
                    results.Add(2);
            });

            await Task.Delay(50);
            results.Add(1);
        }

        await task;

        results.Should().HaveCount(2).And.ContainInOrder(1, 2);
    }
}

public sealed class SemaphoreSlimExtension_LockAsync
{
    [Fact]
    public async Task Locks_execution_Async()
    {
        using SemaphoreSlim mutex = new(1, 1);
        List<int> results = [];
        var task = Task.CompletedTask;

        using (await mutex.LockAsync())
        {
            task = Task.Run(async () =>
            {
                using (await mutex.LockAsync())
                    results.Add(2);
            });

            await Task.Delay(50);
            results.Add(1);
        }

        await task;

        results.Should().HaveCount(2).And.ContainInOrder(1, 2);
    }
}
