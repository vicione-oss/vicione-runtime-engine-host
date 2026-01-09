using System;
using System.Collections.Generic;

namespace ViciOne.ManagedEngine.Runtime;

internal record EngineChainCycleReport(TimeSpan Duration, IReadOnlyDictionary<string, ChainLinkReport> ChainLinkReports);
internal record ChainLinkReport(ulong? Cycle, TimeSpan Duration);
