using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ManagedEngine.PackageResolver;

namespace ViciOne.ManagedEngine.Pipelines.Requests;

internal class DeployEngine : IRequest<string>
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    internal string StartParameter { get; set; } = string.Empty;
    internal IReadOnlyCollection<PackageReference> PackageReferences { get; set; } = [];
    internal int EngineChainIndex { get; set; }
    internal uint CycleTime { get; set; }
}
