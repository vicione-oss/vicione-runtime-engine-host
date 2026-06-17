using System;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace ViciOne.ManagedEngine.TypeResolution;

public class AssemblyLoadContextObserver_Observe
{
    [Fact]
    public async Task Calls_logUnloaded_and_onUnloaded_when_context_dies_before_early_delay()
    {
        var timeProvider = new FakeTimeProvider();
        var logUnloadedCalled = false;
        var onUnloadedCalled = false;
        var alive = true;

        var context = new AssemblyLoadContext("test", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: static () => { },
            logLate: static () => { },
            logUnloaded: () => logUnloadedCalled = true,
            onUnloaded: () => onUnloadedCalled = true,
            logEarlyDelay: 100, logLateDelay: 100, maxWaitDelay: 500, pollInterval: 50,
            timeProvider: timeProvider,
            isAlive: () => alive,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        alive = false;
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await task;

        logUnloadedCalled.Should().BeTrue();
        onUnloadedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Calls_logEarly_then_logUnloaded_when_context_dies_after_early_delay()
    {
        var timeProvider = new FakeTimeProvider();
        var logEarlyCalled = false;
        var logUnloadedCalled = false;
        var onUnloadedCalled = false;
        var alive = true;

        var context = new AssemblyLoadContext("test-early", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: () => logEarlyCalled = true,
            logLate: static () => { },
            logUnloaded: () => logUnloadedCalled = true,
            onUnloaded: () => onUnloadedCalled = true,
            logEarlyDelay: 100, logLateDelay: 200, maxWaitDelay: 500, pollInterval: 50,
            timeProvider: timeProvider,
            isAlive: () => alive,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        // After early delay, context is still alive
        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Yield();

        logEarlyCalled.Should().BeTrue();
        logUnloadedCalled.Should().BeFalse();

        // Context dies before late delay
        alive = false;
        timeProvider.Advance(TimeSpan.FromMilliseconds(200));
        await task;

        logUnloadedCalled.Should().BeTrue();
        onUnloadedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Calls_logLate_then_logUnloaded_when_context_dies_during_polling()
    {
        var timeProvider = new FakeTimeProvider();
        var logEarlyCalled = false;
        var logLateCalled = false;
        var logUnloadedCalled = false;
        var onUnloadedCalled = false;
        var alive = true;

        var context = new AssemblyLoadContext("test-late", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: () => logEarlyCalled = true,
            logLate: () => logLateCalled = true,
            logUnloaded: () => logUnloadedCalled = true,
            onUnloaded: () => onUnloadedCalled = true,
            logEarlyDelay: 50, logLateDelay: 50, maxWaitDelay: 500, pollInterval: 50,
            timeProvider: timeProvider,
            isAlive: () => alive,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        // Advance past early delay
        timeProvider.Advance(TimeSpan.FromMilliseconds(50));
        await Task.Yield();
        logEarlyCalled.Should().BeTrue();

        // Advance past late delay
        timeProvider.Advance(TimeSpan.FromMilliseconds(50));
        await Task.Yield();
        logLateCalled.Should().BeTrue();

        // Context dies during polling
        alive = false;
        timeProvider.Advance(TimeSpan.FromMilliseconds(50));
        await task;

        logUnloadedCalled.Should().BeTrue();
        onUnloadedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Calls_logLeaked_when_context_stays_alive_past_max_wait()
    {
        var timeProvider = new FakeTimeProvider();
        var onUnloadedCalled = false;
        var leakedCalled = false;

        var context = new AssemblyLoadContext("test-leak", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: static () => { },
            logLate: static () => { },
            logUnloaded: static () => { },
            onUnloaded: () => onUnloadedCalled = true,
            logLeaked: () => leakedCalled = true,
            logEarlyDelay: 10, logLateDelay: 10, maxWaitDelay: 50, pollInterval: 10,
            timeProvider: timeProvider,
            isAlive: () => true,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        // Advance past early + late + enough polls to exceed maxWaitDelay
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await task;

        onUnloadedCalled.Should().BeFalse();
        leakedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_call_onUnloaded_when_onUnloaded_is_null()
    {
        var timeProvider = new FakeTimeProvider();
        var logUnloadedCalled = false;

        var context = new AssemblyLoadContext("test-null-callback", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: static () => { },
            logLate: static () => { },
            logUnloaded: () => logUnloadedCalled = true,
            onUnloaded: null,
            logEarlyDelay: 50, logLateDelay: 50, maxWaitDelay: 200, pollInterval: 50,
            timeProvider: timeProvider,
            isAlive: () => false,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        timeProvider.Advance(TimeSpan.FromMilliseconds(50));
        await task;

        logUnloadedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_call_logLeaked_when_logLeaked_is_null()
    {
        var timeProvider = new FakeTimeProvider();

        var context = new AssemblyLoadContext("test-null-leaked", true);
        context.Unload();

        var task = AssemblyLoadContextObserver.Observe(context,
            logEarly: static () => { },
            logLate: static () => { },
            logUnloaded: static () => { },
            onUnloaded: null,
            logLeaked: null,
            logEarlyDelay: 10, logLateDelay: 10, maxWaitDelay: 30, pollInterval: 10,
            timeProvider: timeProvider,
            isAlive: () => true,
            tryEnsureUnload: () => Task.CompletedTask,
            tryEnsureUnloadAggressive: () => Task.CompletedTask);

        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));
        await Task.Yield();
        timeProvider.Advance(TimeSpan.FromMilliseconds(10));

        // Should complete without throwing
        await task;
    }

    [Fact]
    public async Task Does_not_block_unloading_with_real_gc()
    {
        // Integration-style test: uses real WeakReference (no isAlive override)
        var timeProvider = new FakeTimeProvider();
        var unloaded = false;

        CreateAndObserveContext(timeProvider, () => unloaded = true, out var task);

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await task;

        unloaded.Should().BeTrue();

        static void CreateAndObserveContext(FakeTimeProvider tp, Action onUnloaded, out Task observeTask)
        {
            var context = new AssemblyLoadContext("test-gc", true);
            context.Unload();
            observeTask = AssemblyLoadContextObserver.Observe(context,
                logEarly: static () => { },
                logLate: static () => { },
                logUnloaded: onUnloaded,
                logEarlyDelay: 100, logLateDelay: 100,
                timeProvider: tp,
                tryEnsureUnload: () => Task.CompletedTask,
                tryEnsureUnloadAggressive: () => Task.CompletedTask);
        }
    }
}
