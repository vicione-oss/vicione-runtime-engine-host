using System;
using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.Runtime;

internal interface ITimer
{
    TimeSpan Interval { get; }
    void Start();
    Task StopAsync();
}
