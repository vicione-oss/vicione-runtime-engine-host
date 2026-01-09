using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.PackageResolver;
using ViciOne.ManagedEngine.Runtime;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class GetPackagesHandler(IDeploymentPool deploymentPool, TransactionContext transactionContext)
    : IRequestHandler<GetPackages, IReadOnlyCollection<PackageReference>>
{
    private readonly IDeploymentPool _deploymentPool = deploymentPool;
    private readonly TransactionContext _transactionContext = transactionContext;

    public async Task<IReadOnlyCollection<PackageReference>> Handle(GetPackages request, CancellationToken cancellationToken)
    {
        using var _ = await _transactionContext.WaitAsync(request.DeploymentIdentifier, TransactionContext.Timeout).ConfigureAwait(false);

        if (!_deploymentPool.TryGetDeployment(request.DeploymentIdentifier, out var _))
            throw new InvalidOperationException($"Cannot find deployment '{request.DeploymentIdentifier}'.");

        var deployParameter = await _deploymentPool.GetDeployParameter(request.DeploymentIdentifier, cancellationToken);

        return deployParameter.PackageReferences;
    }
}
