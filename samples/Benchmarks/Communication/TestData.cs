using System.Collections.Generic;

namespace Benchmarks.Communication;

internal sealed class TestData<T>
{
    internal List<EngineCycle<T>> EngineCycles { get; } = [];
}
