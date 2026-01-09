using System.Collections.Generic;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.EngineDeployment;

internal class DeployParameter
{
    public IReadOnlyCollection<PackageReference> PackageReferences { get; set; } = [];
    public int EngineChainIndex { get; set; }
    public uint CycleTime { get; set; }
}
