using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Pipelines;

[DebuggerStepThrough]
internal static class CommandStatusExtensions
{
    extension(Task<CommandStatus> task)
    {
        internal async Task OnFailureAsync(Func<string, Task> func)
        {
            var status = await task.ConfigureAwait(false);
            if (status is Failure failure)
                await func(failure.Message).ConfigureAwait(false);
        }
    }
}
