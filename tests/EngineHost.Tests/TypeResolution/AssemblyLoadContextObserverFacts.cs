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
            AssemblyLoadContextObserver.Observe(context, stillAlive, stillAlive, unloaded, 100, 1);
        }
    }
}
