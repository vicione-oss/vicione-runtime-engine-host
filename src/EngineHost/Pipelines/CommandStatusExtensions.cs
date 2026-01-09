using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Pipelines;

[DebuggerStepThrough]
internal static class CommandStatusExtensions
{
    internal static async Task OnFailureAsync(this Task<CommandStatus> task, Func<string, Task> func)
    {
        var status = await task.ConfigureAwait(false);
        if (status is Failure failure)
            await func(failure.Message).ConfigureAwait(false);
    }
}
