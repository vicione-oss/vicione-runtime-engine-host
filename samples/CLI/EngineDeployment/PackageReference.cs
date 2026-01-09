using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ManagedEngine.EngineDeployment;

[SuppressMessage("Performance", "CA1812:Nicht instanziierte interne Klassen vermeiden.", Justification = "Implementationen von IDeployParameterFactory nutzen diesen Typ")]
internal sealed record class PackageReference
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
