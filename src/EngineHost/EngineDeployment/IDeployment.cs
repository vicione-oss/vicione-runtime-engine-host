using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal interface IDeployment : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<EngineState> GetStateAsync(CancellationToken cancellationToken);
    ulong ProcessCycle();
}
