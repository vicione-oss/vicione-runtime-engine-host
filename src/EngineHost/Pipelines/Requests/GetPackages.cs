using System.Collections.Generic;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class GetPackages : IRequest<IReadOnlyCollection<PackageReference>>
{
    public string DeploymentIdentifier { get; set; } = string.Empty;
}
