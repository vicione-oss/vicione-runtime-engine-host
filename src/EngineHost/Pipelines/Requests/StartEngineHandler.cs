using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal sealed class StartEngineHandler(IDeploymentPool deploymentPool, IEngineChain engineChain, TransactionContext transactionContext)
    : IRequestHandler<StartEngine, bool>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly IEngineChain _engineChain = engineChain;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<bool> Handle(StartEngine request, CancellationToken cancellationToken)
    {
        if (!_deploymentPool.DeploymentIsStarted(request.DeploymentIdentifier) &&
            _deploymentPool.TryGetDeployment(request.DeploymentIdentifier, out var deployment))
        {
            using var _ = await _transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);
            await deployment.StartAsync(cancellationToken).ConfigureAwait(false);
            _engineChain.EnableChainLink(request.DeploymentIdentifier);
            _deploymentPool.DeploymentStarted(request.DeploymentIdentifier);
            return true;
        }
        return false;
    }
}
