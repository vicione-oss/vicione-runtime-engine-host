using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

public static class FuncExtensions
{
    public static async Task WaitForTrue(this Func<bool> condition, int timeoutMs = 5000)
    {
        using CancellationTokenSource cts = new(timeoutMs);
        while (!condition())
            await Task.Delay(10, cts.Token);
    }
}
