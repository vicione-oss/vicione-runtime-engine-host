using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal sealed class StopEngineHandler(IDeploymentPool deploymentPool, IEngineChain engineChain, TransactionContext transactionContext)
    : IRequestHandler<StopEngine, bool>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly IEngineChain _engineChain = engineChain;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<bool> Handle(StopEngine request, CancellationToken cancellationToken)
    {
        if (_deploymentPool.DeploymentIsStarted(request.DeploymentIdentifier) &&
            _deploymentPool.TryGetDeployment(request.DeploymentIdentifier, out var deployment))
        {
            using var _ = await _transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);
            _engineChain.DisableChainLink(request.DeploymentIdentifier);
            await deployment.StopAsync(cancellationToken).ConfigureAwait(false);
            _deploymentPool.DeploymentStopped(request.DeploymentIdentifier);
            return true;
        }
        return false;
    }
}
