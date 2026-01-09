using System.Collections.Generic;
using System.Text.Json;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Communication;

internal class DeployEnginePayload
{
    public JsonElement StartParameter { get; set; }
    public List<PackageReference> PackageReferences { get; set; } = [];
    public int EngineChainIndex { get; set; }
    public uint CycleTime { get; set; }
}
