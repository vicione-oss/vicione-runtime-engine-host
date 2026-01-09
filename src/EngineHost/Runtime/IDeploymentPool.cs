using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ManagedEngine.EngineDeployment;

namespace ViciOne.ManagedEngine.Runtime;

internal interface IDeploymentPool
{
    bool TryGetDeployment(string id, [NotNullWhen(true)] out IDeployment? deployment);
    Task<IDeployment> CreateDeploymentAsync(string deploymentId, AssemblyLoadContext context, string startParameter, DeployParameter deployParameter, CancellationToken cancellationToken);
    Task<IDeployment> RecoverDeploymentAsync(string deploymentId, AssemblyLoadContext context, CancellationToken cancellationToken);
    Task RemoveDeploymentAsync(string id);
    Task<DeployParameter> GetDeployParameter(string deploymentId, CancellationToken cancellationToken);
    HashSet<string> GetStartedDeployments();
    HashSet<string> GetDeployments();
    void DeploymentStarted(string id);
    void DeploymentStopped(string id);
    bool DeploymentIsStarted(string id);
}
