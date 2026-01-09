using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Runtime;

internal interface IContextPool
{
    Task<AssemblyLoadContext> RegisterDeploymentAsync(string deploymentId, IReadOnlyCollection<PackageReference> packageReferences, CancellationToken cancellationToken);
    Task UnregisterDeploymentAsync(string id);
}
