using System;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal interface IEngineChain
{
    event Func<EngineChainCycleReport, Task>? CycleReport;
    event Func<EngineChainCrashReport, Task>? CrashReport;

    Task StopAsync();
    Task SetUpCycleTimeAsync(TimeSpan cycleTime);
    void AddChainLink(string id, Func<ulong> chainLink, int index);
    Task RemoveChainLinkAsync(string id);
    void EnableChainLink(string id);
    void DisableChainLink(string id);
}
