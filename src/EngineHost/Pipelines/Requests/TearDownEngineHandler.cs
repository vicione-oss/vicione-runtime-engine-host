using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal sealed class TearDownEngineHandler(IDeploymentPool deploymentPool, IEngineChain engineChain, IContextPool contextPool, TransactionContext transactionContext)
    : IRequestHandler<TearDownEngine, bool>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly IEngineChain _engineChain = engineChain;
    private readonly IContextPool _contextPool = contextPool;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<bool> Handle(TearDownEngine request, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = await _transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);
            await _engineChain.RemoveChainLinkAsync(request.DeploymentIdentifier).ConfigureAwait(false);
            await _deploymentPool.RemoveDeploymentAsync(request.DeploymentIdentifier).ConfigureAwait(false);
            await _contextPool.UnregisterDeploymentAsync(request.DeploymentIdentifier).ConfigureAwait(false);
        }
        finally
        {
            _transactionContext.Unregister(request.DeploymentIdentifier);
        }
        return true;
    }
}
