using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class GetStateHandler(IDeploymentPool deploymentPool, TransactionContext transactionContext)
    : IRequestHandler<GetState, EngineState>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<EngineState> Handle(GetState request, CancellationToken cancellationToken)
    {
        using var _ = await _transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);
        if (_deploymentPool.TryGetDeployment(request.DeploymentIdentifier, out var deployment))
            return await deployment.GetStateAsync(cancellationToken).ConfigureAwait(false);
        throw new InvalidOperationException($"Cannot find deployment '{request.DeploymentIdentifier}'.");
    }
}
