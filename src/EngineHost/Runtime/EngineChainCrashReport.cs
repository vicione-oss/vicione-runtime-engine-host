using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Runtime;

internal record EngineChainCrashReport(IReadOnlyCollection<string> CrashedChainLinks);
