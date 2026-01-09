using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ManagedEngine.EngineDeployment;

[SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Implementationen von IDeployParameterFactory nutzen diesen Typ")]
internal sealed record class DeployParameter
{
    public StartParameter StartParameter { get; set; } = new();
    public IReadOnlyCollection<PackageReference> PackageReferences { get; set; } = [];
    public int EngineChainIndex { get; set; }
    public uint CycleTime { get; set; }
}
