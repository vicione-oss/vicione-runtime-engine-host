using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal sealed class GetDeploymentStatesHandler(IDeploymentPool deploymentPool, TransactionContext transactionContext)
    : IRequestHandler<GetDeploymentStates, IReadOnlyDictionary<string, EngineState>>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<IReadOnlyDictionary<string, EngineState>> Handle(GetDeploymentStates request, CancellationToken cancellationToken)
    {
        var deploymentStates = new Dictionary<string, EngineState>();
        var deployments = _deploymentPool.GetDeployments();

        foreach (var deploymentName in deployments)
        {
            using var _ = await _transactionContext.WaitAsync(deploymentName, TransactionContext.Timeout).ConfigureAwait(false);

            if (_deploymentPool.TryGetDeployment(deploymentName, out var deployment))
                deploymentStates.Add(deploymentName, await deployment.GetStateAsync(cancellationToken));
        }

        return deploymentStates;
    }
}
