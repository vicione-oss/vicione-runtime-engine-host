using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal sealed class DeployEngineHandler(IDeploymentPool deploymentPool, IContextPool contextPool, IEngineChain engineChain, TransactionContext transactionContext)
    : IRequestHandler<DeployEngine, string>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly IContextPool _contextPool = contextPool;
    private readonly IEngineChain _engineChain = engineChain;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<string> Handle(DeployEngine request, CancellationToken cancellationToken)
    {
        var deploymentId = GenerateDeploymentId();

        try
        {
            var context = await _contextPool.RegisterDeploymentAsync(deploymentId, request.PackageReferences, cancellationToken)
                .ConfigureAwait(false);
            var deployment = await _deploymentPool.CreateDeploymentAsync(deploymentId, context, request.StartParameter,
                new() { PackageReferences = request.PackageReferences, CycleTime = request.CycleTime, EngineChainIndex = request.EngineChainIndex, },
                cancellationToken).ConfigureAwait(false);

            await _engineChain.SetUpCycleTimeAsync(TimeSpan.FromMilliseconds(request.CycleTime)).ConfigureAwait(false);
            _engineChain.AddChainLink(deploymentId, deployment.ProcessCycle, request.EngineChainIndex);
        }
        catch
        {
            await _deploymentPool.RemoveDeploymentAsync(deploymentId).ConfigureAwait(false);
            await _contextPool.UnregisterDeploymentAsync(deploymentId).ConfigureAwait(false);

            throw;
        }

        _transactionContext.Register(deploymentId);

        return deploymentId;

        static string GenerateDeploymentId()
            => Guid.NewGuid().ToString();
    }
}
