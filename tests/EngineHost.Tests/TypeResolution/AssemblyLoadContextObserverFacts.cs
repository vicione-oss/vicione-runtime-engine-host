using System;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.ManagedEngine.TypeResolution;

public class AssemblyLoadContextObserver_Observe
{
    [Fact]
    public async Task Does_not_block_unloading_Async()
    {
        var alive = false;

        CreateAndObserveContext(() => alive = true, () => alive = false);
        TryToClearCache();

        await Task.Delay(150, TestContext.Current.CancellationToken);

        alive.Should().BeFalse();

        static void TryToClearCache()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        static void CreateAndObserveContext(Action stillAlive, Action unloaded)
        {
            var context = new AssemblyLoadContext("test", true);
            context.Unload();
            AssemblyLoadContextObserver.Observe(context, stillAlive, stillAlive, unloaded, logEarlyDelay: 100, logLateDelay: 1);
        }
    }

    [Fact]
    public async Task Invokes_onUnloaded_only_after_context_is_collected_Async()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var onUnloadedCalled = false;

        CreateAndObserveContext(() => onUnloadedCalled = true, tcs);
        TryToClearCache();

        await Task.Delay(200, TestContext.Current.CancellationToken);

        onUnloadedCalled.Should().BeTrue();
        tcs.Task.IsCompleted.Should().BeTrue();

        static void TryToClearCache()
        {
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        static void CreateAndObserveContext(Action onUnloaded, TaskCompletionSource tcs)
        {
            var context = new AssemblyLoadContext("test-onunloaded", true);
            context.Unload();
            AssemblyLoadContextObserver.Observe(context,
                logEarly: static () => { },
                logLate: static () => { },
                logUnloaded: static () => { },
                onUnloaded: () => { onUnloaded(); tcs.SetResult(); },
                logEarlyDelay: 50, logLateDelay: 1);
        }
    }

    [Fact]
    public async Task Does_not_invoke_onUnloaded_when_max_wait_elapses_Async()
    {
        var onUnloadedCalled = false;
        var leakedCalled = false;

        // Keep strong reference so the ALC never collects
        var context = new AssemblyLoadContext("test-leak", true);
        context.Unload();

        AssemblyLoadContextObserver.Observe(context,
            logEarly: static () => { },
            logLate: static () => { },
            logUnloaded: static () => { },
            onUnloaded: () => onUnloadedCalled = true,
            logLeaked: () => leakedCalled = true,
            logEarlyDelay: 10, logLateDelay: 10, maxWaitDelay: 50, pollInterval: 10);

        await Task.Delay(300, TestContext.Current.CancellationToken);

        onUnloadedCalled.Should().BeFalse();
        leakedCalled.Should().BeTrue();

        GC.KeepAlive(context);
    }
}
