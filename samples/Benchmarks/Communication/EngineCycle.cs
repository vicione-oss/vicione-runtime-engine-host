using System.Collections.Generic;

namespace Benchmarks.Communication;

internal sealed class EngineCycle<T>(ulong number)
{
    internal ulong Number { get; init; } = number;
    internal List<T> Values { get; } = [];
}
