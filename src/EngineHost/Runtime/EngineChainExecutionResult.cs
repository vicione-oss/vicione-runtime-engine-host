using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Runtime;

internal sealed class EngineChainExecutionResult
{
    internal Dictionary<string, ChainLinkReport> ChainLinkDurations { get; } = [];
    internal List<string> CrashedChainLinks { get; } = [];
}
