using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal static class AsyncEventHandlerExtensions
{
    internal static async Task InvokeAsync<TEventArgs>(this Func<TEventArgs, Task>? @event, TEventArgs args)
    {
        if (@event is not null)
        {
            var handlers = @event.GetInvocationList().OfType<Func<TEventArgs, Task>>();
            List<Exception>? exceptions = null;

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(args).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= [];
                    exceptions.Add(ex);
                }
            }

            if (exceptions is not null)
                throw new AggregateException(exceptions);
        }
    }
}
